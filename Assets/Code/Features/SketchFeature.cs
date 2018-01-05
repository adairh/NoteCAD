﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

public class SketchFeature : Feature {
	EquationSystem sys = new EquationSystem();
	List<List<Entity>> loops = new List<List<Entity>>();
	protected LineCanvas canvas;
	Sketch sketch;
	Mesh mainMesh;
	GameObject go;
	
	public Sketch GetSketch() {
		return sketch;
	}

	public SketchFeature() {
		sketch = new Sketch();
		canvas = GameObject.Instantiate(EntityConfig.instance.lineCanvas);
		mainMesh = new Mesh();
		go = new GameObject("SketchFeature");
		var mf = go.AddComponent<MeshFilter>();
		var mr = go.AddComponent<MeshRenderer>();
		mf.mesh = mainMesh;
		mr.material = EntityConfig.instance.meshMaterial;
		canvas.parent = go;
	}

	public void AddDrag(Exp drag) {
		sys.AddEquation(drag);
	}

	public void RemoveDrag(Exp drag) {
		sys.RemoveEquation(drag);
	}

	void UpdateSystem() {
		sys.Clear();
		sketch.GenerateEquations(sys);
	}

	protected override void OnUpdateDirty() {
		if(sketch.topologyChanged || sketch.constraintsTopologyChanged) {
			UpdateSystem();
		}

		if(sketch.topologyChanged) {
			loops = sketch.GenerateLoops();
		}

		if(sketch.IsConstraintsChanged()) {
			canvas.ClearStyle("constraints");
			canvas.SetStyle("constraints");
			foreach(var c in sketch.constraintList) {
				c.Draw(canvas);
			}
		}

		if(sketch.IsEntitiesChanged()) {
			canvas.ClearStyle("entities");
			canvas.SetStyle("entities");
			foreach(var e in sketch.entityList) {
				e.Draw(canvas);
			}

			canvas.ClearStyle("error");
			canvas.SetStyle("error");
			foreach(var e in sketch.entityList) {
				if(!e.isError) continue;
				e.Draw(canvas);
			}
		}

		var loopsChanged = loops.Any(l => l.Any(e => e.IsChanged()));
		if(loopsChanged || sketch.topologyChanged) {
			CreateLoops();
		}
		sketch.MarkUnchanged();
		canvas.UpdateDirty();
	}

	protected override SketchObject OnHover(Vector3 mouse, Camera camera, ref double objDist) {
		return sketch.Hover(mouse, camera, ref objDist);
	}

	protected override void OnUpdate() {
		string result = sys.Solve().ToString();
		result += "\n" + sys.stats;
		DetailEditor.instance.resultText.text = result.ToString();

		if(sketch.IsConstraintsChanged() || sketch.IsEntitiesChanged() || sketch.IsDirty()) {
			MarkDirty();
		}
	}

	public override GameObject gameObject {
		get {
			return go;
		}
	}

	void CreateLoops() {
		var itr = new Vector3();
		foreach(var loop in loops) {
			loop.ForEach(e => e.isError = false);
			foreach(var e0 in loop) {
				foreach(var e1 in loop) {
					if(e0 == e1) continue;
					var cross = e0.IsCrossed(e1, ref itr);
					e0.isError = e0.isError || cross;
					e1.isError = e1.isError || cross;
				}
			}
		}
		var polygons = Sketch.GetPolygons(loops.Where(l => l.All(e => !e.isError)).ToList());
		if(mainMesh == null) {
		}
		mainMesh.Clear();
		MeshUtils.CreateMeshRegion(polygons, ref mainMesh);
		if(go == null) {
		}
	}

	protected override void OnWrite(XmlTextWriter xml) {
		sketch.Write(xml);
	}

	protected override void OnRead(XmlNode xml) {
		sketch.Read(xml);
	}


	protected override void OnClear() {
		sketch.Clear();
		Update();
	}

	public List<List<Entity>> GetLoops() {
		return loops;
	}

	public override bool ShouldHoverWhenInactive() {
		return false;
	}

	protected override void OnActivate(bool state) {
		go.SetActive(state);
	}
}
