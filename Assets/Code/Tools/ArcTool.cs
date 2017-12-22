﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcTool : Tool {

	ArcEntity current;
	bool canCreate = true;

	protected override void OnMouseDown(Vector3 pos, SketchObject sko) {

		if(current != null) {
			if(!canCreate) return;
			current.p1.pos = pos;
			current.p1.isSelectable = true;
			current.p0.isSelectable = true;
			current.c.isSelectable = true;
			current.isSelectable = true;
			if(AutoConstrainCoincident(current.p1, sko as Entity)) {
				current = null;
				StopTool();
				return;
			}
		}

		var newEntity = new ArcEntity(Sketch.instance);
		newEntity.p0.pos = pos;
		newEntity.p1.pos = pos;
		newEntity.c.pos = pos;
		if(current == null) {
			AutoConstrainCoincident(newEntity.p0, sko as Entity);
		} else {
			new PointsCoincident(current.sketch, current.p1, newEntity.p0);
		}

		current = newEntity;
		current.isSelectable = false;
		current.p0.isSelectable = false;
		current.p1.isSelectable = false;
		current.c.isSelectable = false;
	}

	protected override void OnMouseMove(Vector3 pos, SketchObject entity) {
		if(current != null) {
			current.p1.pos = pos;
			current.c.pos = (current.p0.pos + current.p1.pos) * 0.5f;
			var itr = new Vector3();
			canCreate = true;//!current.sketch.IsCrossed(current, ref itr);
			current.isError = !canCreate;
		} else {
			canCreate = true;
		}
	}

	protected override void OnDeactivate() {
		if(current != null) {
			current.Destroy();
			current = null;
		}
		canCreate = true;
	}

}
