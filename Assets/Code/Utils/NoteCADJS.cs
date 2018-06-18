﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class NoteCADJS : MonoBehaviour {

#if UNITY_EDITOR

	public static void SaveData(string data, string filename) {
		var path = UnityEditor.EditorUtility.SaveFilePanel("Save NoteCAD file", "", filename, "");
		System.IO.File.WriteAllText(path, data);
	}

	public static void LoadData(Action<string> callback) {
		var path = UnityEditor.EditorUtility.OpenFilePanel("Load NoteCAD file", "", "");
		callback(System.IO.File.ReadAllText(path));
	}

	public static void LoadBinaryData(Action<byte[]> callback) {
		var path = UnityEditor.EditorUtility.OpenFilePanel("Load NoteCAD file", "", "");
		callback(System.IO.File.ReadAllBytes(path));
	}
	
#elif UNITY_WEBGL
	
	[DllImport("__Internal")]
	public static extern void SaveData(string data, string filename);


	[DllImport("__Internal")]
	private static extern string LoadDataInternal();
	private static Action<string> loadCallback;
	public static void LoadData(Action<string> callback) {
		loadCallback = callback;
		LoadDataInternal();
	}
	public void LoadDataCallback(string d) {
		if(loadCallback != null) {
			loadCallback(d);
			loadCallback = null;
		}
	}

	[DllImport("__Internal")]
	private static extern void LoadBinaryDataInternal();
	private static Action<byte[]> loadBinaryCallback;
	public static void LoadBinaryData(Action<byte[]> callback) {
		loadBinaryCallback = callback;
		LoadBinaryDataInternal();
	}

	void BinaryFileSelected(string url) {
		StartCoroutine(LoadBinaryCoroutine(url));
	}
 
	IEnumerator LoadBinaryCoroutine(string url) {
		WWW www = new WWW(url);
		yield return www;
		loadBinaryCallback(www.bytes);
	}

#else
	
	public static void SaveData(string data, string filename) {}
	public static void LoadData(Action<string> callback) {}
	public static void LoadBinaryData(Action<byte[]> callback) {}

#endif

#if UNITY_WEBGL && !UNITY_EDITOR
	[DllImport("__Internal")]
	public static extern string GetParam(string name);
#else
	public static string GetParam(string name) {
		return "";
	}
#endif

}
