﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointBehaviour : EntityBehaviour {

	void Update() {
		var point = entity as PointEntity;
		transform.position = point.GetPosition();
	}

}
