﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Csg;
using RuntimeInspectorNamespace;

public class DetailEditor : MonoBehaviour {

	static DetailEditor instance_;

	public static DetailEditor instance {
		get {
			if(instance_ == null) {
				instance_ = FindObjectOfType<DetailEditor>();
			}
			return instance_;
		}
	}

	Detail detail;

	public GameObject labelParent;
	public Text resultText;
	public GameObject featuresContent;
	public FeatureUI featureUIPrefab;
	public List<FeatureUI> featuresUI;
	public Color pressedColor;
	public Mesh mesh;
	public Mesh selectedMesh;
	public Solid solid;
	public RuntimeInspector inspector;

	bool meshDirty = true;

	LineCanvas canvas;
	EquationSystem sys = new EquationSystem();
	public List<IdPath> selection = new List<IdPath>();

	ICADObject hovered_;
	public ICADObject hovered {
		get {
			return hovered_;
		}
		set {
			if(hovered_ == value) return;
			if(hovered_ != null) {
				if(hovered_ is SketchObject) {
					(hovered_ as SketchObject).isHovered = false;
				}
			}
			hovered_ = value;
			if(hovered_ != null) {
				var id = hovered_.id;
				//Debug.Log(id.ToString());
				//var hh = detail.GetObjectById(id);
				//Debug.Log(id.ToString());
				//Debug.Log(id.ToString() + " " + hh.GetType().Name);
				if(hovered_ is SketchObject) {
					(hovered_ as SketchObject).isHovered = true;
				}
			}
		}
	}

	public SketchFeatureBase currentSketch {
		get {
			return activeFeature as SketchFeatureBase;
		}
		set {
			ActivateFeature(value);
		}
	}

	public SketchFeature currentWorkplane {
		get {
			return activeFeature as SketchFeature;
		}
		set {
			ActivateFeature(value);
		}
	}

	Feature activeFeature_;
	public Feature activeFeature {
		get {
			return activeFeature_;
		}
		set {
			ActivateFeature(value);
		}
	}
	
	IEnumerator LoadWWWFile(string url) {
		WWW www = new WWW(url);
		yield return www;
		ReadXml(www.text);
	}

	public bool IsFirstMeshFeature(MeshFeature mf) {
		var fi = detail.features.FindIndex(f => f is MeshFeature);
		var mi = detail.features.IndexOf(mf);
		return fi == mi;
	}

	GameObject CreateMeshObject(string name, Mesh mesh, Material material) {
		var go = new GameObject(name);
		var mf = go.AddComponent<MeshFilter>();
		var mr = go.AddComponent<MeshRenderer>();
		mesh.name = name;
		mf.mesh = mesh;
		mr.material = material;
		return go;
	}

	private void Start() {
		instance_ = this;
		mesh = new Mesh();
		selectedMesh = new Mesh();
		CreateMeshObject("DetailMesh", mesh, EntityConfig.instance.meshMaterial);
		CreateMeshObject("DetailMeshSelection", selectedMesh, EntityConfig.instance.loopMaterial);
		New();
		if(NoteCADJS.GetParam("filename") != "") {
			var uri = new Uri(Application.absoluteURL);
			var url = "http://" + uri.Host + ":" + uri.Port + "/Files/" + NoteCADJS.GetParam("filename");
			StartCoroutine(LoadWWWFile(url));
		}
		canvas = GameObject.Instantiate(EntityConfig.instance.lineCanvas);
	}

	void UpdateFeatures() {
		for(int i = featuresUI.Count - 1; i >= 0; i--) {
			Destroy(featuresUI[i].gameObject);
		}
		featuresUI.Clear();
		foreach(var f in detail.features) {
			var ui = Instantiate(featureUIPrefab, featuresContent.transform);
			ui.feature = f;
			featuresUI.Add(ui);
		}
		ActivateFeature(activeFeature);
	}

	public void AddDrag(Exp drag) {
		sys.AddEquation(drag);
	}

	public void RemoveDrag(Exp drag) {
		sys.RemoveEquation(drag);
	}

	void UpdateSystem() {
		sys.Clear();
		activeFeature.GenerateEquations(sys);
	}
	string dofText;

	public bool suppressCombine = false;
	public bool suppressHovering = false;

	private void Update() {
		if(activeFeature != null) {
			if(currentSketch != null && currentSketch.IsTopologyChanged()) {
				UpdateSystem();
			}
			var res = sys.Solve();
			string result = "";
			result += (GC.GetTotalMemory(false) / 1024 / 1024.0).ToString("0.##") + " mb\n";
			result += res.ToString() + "\n";
			if(sys.dofChanged) {
				if(res == EquationSystem.SolveResult.OKAY && !sys.HasDragged()) {
					int dof;
					bool ok = sys.TestRank(out dof);
					if(!ok) {
						dofText = "<color=\"#FF3030\">DOF: " + dof + "</color>\n";
					} else if(dof == 0) {
						dofText = "<color=\"#30FF30\">DOF: " + dof + "</color>\n";
					} else {
						dofText = "<color=\"#FFFFFF\">DOF: " + dof + "</color>\n";
					}
				} else {
					dofText = "<color=\"#303030\">DOF: ?</color>\n";
				}
			}
			result += dofText;
			//result += sys.stats;
			resultText.text = result.ToString();
		}

		detail.Update();
		meshDirty = meshDirty | detail.features.OfType<MeshFeature>().Any(f => f.dirty);
		detail.MarkDirty();
		detail.UpdateDirtyUntil(activeFeature);
		if(meshDirty && !suppressCombine) {
			meshDirty = false;
			mesh.Clear();
			Solid result = null;
			int combinedCount = 0;
			foreach(var f in detail.features) {
				var mf = f as MeshFeature;
				if(mf != null) {
					if(result == null) {
						result = mf.solid;
					} else {
						if(mf.combined == null) {
							//#if UNITY_WEBGL
								//if(combinedCount > 0) {
								//	break;
								//}
							//#endif
							switch(mf.operation) {
								case CombineOp.Union: mf.combined = Solids.Union(result, mf.solid); break;
								case CombineOp.Difference: mf.combined = Solids.Difference(result, mf.solid); break;
								case CombineOp.Intersection: mf.combined = Solids.Intersection(result, mf.solid); break;
							}
							combinedCount++;
						}
						result = mf.combined;
					}
				}
				if(f == activeFeature) break;
			}
			Debug.Log("combined " + combinedCount + " meshes");
			solid = result;
			if(result != null) {
				mesh.FromSolid(result);
			}
		}
		
		
		if(!CameraController.instance.IsMoving && !suppressHovering) {
			double dist = -1.0;
			hovered = detail.HoverUntil(Input.mousePosition, Camera.main, UnityEngine.Matrix4x4.identity, ref dist, activeFeature);
			/*
			if(hovered == null && solid != null) {
				var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				var id = solid.Raytrace(ray);
				selectedMesh.FromSolid(solid, id);
			} else {
				selectedMesh.Clear();
			}*/
		}

		canvas.ClearStyle("hovered");
		canvas.ClearStyle("hoveredPoints");
		if(hovered != null) {
			DrawCadObject(hovered, "hovered");
		}

		canvas.ClearStyle("selected");
		canvas.ClearStyle("selectedPoints");
		foreach(var idp in selection) {
			var obj = detail.GetObjectById(idp);
			if(obj == null) continue;
			DrawCadObject(obj, "selected");
		}

		if(selection.Count == 1) {
			var obj = detail.GetObjectById(selection[0]);
			inspector.Inspect(obj);
		} else {
			inspector.Inspect(activeFeature);
		}

		if(activeFeature is SketchFeatureBase) {
			var sk = activeFeature as SketchFeatureBase;
			sk.DrawConstraints(canvas);
		} else {
			canvas.ClearStyle("constraints");
		}
	}

	void DrawCadObject(ICADObject obj, string style) {
		var he = obj as IEntity;
		canvas.SetStyle((he != null && he.type == IEntityType.Point) ? style + "Points" : style);
		if(he != null) {
			canvas.DrawSegments((obj as IEntity).SegmentsInPlane(null));
		} else
		if(obj is SketchObject) {
			(obj as SketchObject).Draw(canvas);
		}
	}

	public bool RemoveById(IdPath idp) {
		var obj = detail.GetObjectById(idp);
		if(obj is SketchObject) {
			var sko = obj as SketchObject;
			sko.Destroy();
			return true;
		}
		return false;
	}

	private void LateUpdate() {
		detail.Draw(UnityEngine.Matrix4x4.identity);
		GC.Collect();
	}

	private void OnGUI() {
		GUIStyle style = new GUIStyle();
		style.alignment = TextAnchor.MiddleCenter;
		if(activeFeature is SketchFeatureBase) {
			var sk = activeFeature as SketchFeatureBase;
			foreach(var c in sk.GetSketch().constraintList) {
				if(!(c is ValueConstraint)) continue;
				var constraint = c as ValueConstraint;
				if(!constraint.valueVisible) continue;
				if(hovered == c) {
					style.normal.textColor = canvas.GetStyle("hovered").color;
				} else {
					style.normal.textColor = Color.white;
				}
				var pos = constraint.pos;
				pos = Camera.main.WorldToScreenPoint(pos);
				var txt = constraint.GetLabel();
				GUI.Label(new Rect(pos.x, Camera.main.pixelHeight - pos.y, 0, 0), txt, style);
			}
		}
	}
		

	public void New() {
		if(detail != null) {
			detail.Clear();
		}
		selection.Clear();
		activeFeature = null;
		detail = new Detail();
		var sk = new SketchFeature();
		sk.shouldHoverWhenInactive = true;
		new PointEntity(sk.GetSketch());
		detail.AddFeature(sk);
		sk = new SketchFeature();
		detail.AddFeature(sk);
		UpdateFeatures();
		ActivateFeature(sk);
	}

	public void ReadXml(string xml) {
		activeFeature = null;
		detail.ReadXml(xml);
		UpdateFeatures();
		ActivateFeature(detail.features.Last());
	}

	public string WriteXml() {
		return detail.WriteXml();
	}

	public void AddFeature(Feature feature) {
		detail.AddFeature(feature);
		meshDirty = true;
		UpdateFeatures();
	}

	public void ActivateFeature(Feature feature) {
		bool skipActive = (activeFeature_ == feature);
		if(activeFeature_ != null) {
			var ui = featuresUI.Find(u => u.feature == activeFeature_);
			var btn = ui.GetComponent<Button>();
			var cb = btn.colors;
			cb.normalColor = Color.white;
			btn.colors = cb;
			if(!skipActive) activeFeature_.active = false;
		}
		activeFeature_ = feature;
		if(activeFeature_ != null) {
			var ui = featuresUI.Find(u => u.feature == activeFeature_);
			var btn = ui.GetComponent<Button>();
			var cb = btn.colors;
			cb.normalColor = pressedColor;
			btn.colors = cb;
			if(!skipActive) activeFeature_.active = true;
			inspector.Inspect(activeFeature_);
			UpdateSystem();
		}
		meshDirty = true;
		if(detail != null) {
			var visible = true;
			foreach(var f in detail.features) {
				f.visible = visible;
				if(f == activeFeature_) {
					visible = false;
				}
			}
		}
	}

	public string ExportSTL() {
		return mesh.ExportSTL();
	}

	public string ExportCurrentSTL() {
		if(activeFeature is MeshFeature) {
			return (activeFeature as MeshFeature).solid.ToStlString(activeFeature.GetType().Name);
		}
		return "";
	}

	private void OnDrawGizmos() {
		if(currentSketch != null) {
			var bounds = currentSketch.bounds;
			if(currentSketch is LinearArrayFeature) {
				var laf = currentSketch as LinearArrayFeature;
				laf.DrawGizmos(Input.mousePosition, Camera.main);
			} else {
				Gizmos.DrawWireCube(bounds.center, bounds.size);
			}
		}

	}
}
