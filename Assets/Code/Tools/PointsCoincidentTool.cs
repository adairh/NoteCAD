﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointsCoincidentTool : Tool {

	PointEntity p0;

	protected override void OnMouseDown(Vector3 pos, ICADObject ico) {
		IEntity entity = ico as IEntity;
		if(entity == null) return;
		if(p0 != null) {
			if(entity is PointEntity) {
				var p = entity as PointEntity;
				new PointsCoincident(DetailEditor.instance.currentSketch.GetSketch(), p0, p);
			} else 
			if(entity.type == IEntityType.Line) {
				new PointOnLine(DetailEditor.instance.currentSketch.GetSketch(), p0, entity);
			}
			p0 = null;
		} else if(entity is PointEntity) {
			p0 = entity as PointEntity;
		}
	}

	protected override void OnDeactivate() {
		p0 = null;
	}

	protected override string OnGetDescription() {
		return "hover and click two different points to constrain them to be coincident.";
	}

}
