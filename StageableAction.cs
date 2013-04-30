/*
 * Created by SharpDevelop.
 * User: Bernhard
 * Date: 19.04.2013
 * Time: 18:56
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class StageableAction : PartModule
{
	private static int id;
	
	[KSPField(isPersistant = true, guiActive = true, guiName = "Groups")]
	public string groups;
	[KSPField(isPersistant = true, guiActive = true, guiName = "Altitude")]
	public int altitude;
	[KSPField(isPersistant = true, guiActive = false, guiName = "Delay")]
	public float delay;

	[KSPField(isPersistant = false, guiActive = true, guiName = "Triggered")]
	private bool triggered = false;
	[KSPField(isPersistant = false, guiActive = true, guiName = "Alt Armed")]
	private bool armed = false;
	
	[KSPEvent(guiActive = true, guiName = "Edit", active = true)]
	public void Edit()
	{
		window = true;
	}

	private List<string> _groups = new List<string>();
	private bool window = false;
	private bool stage = false;
	private Rect winPosSize = new Rect(275, 25, 120, 50);
	private string[] custom = new string[] {"Throttle Full", "Throttle None"};
	
	private void UpdateGroups()
	{
		groups = string.Join(",", _groups.ToArray());
	}
	
	public override void OnStart(PartModule.StartState state)
	{
		id = DateTime.Now.Millisecond;
		RenderingManager.AddToPostDrawQueue (0, new Callback (drawGUI));
		if (state == StartState.Editor) {
			part.OnEditorAttach += delegate { window = true; };
			part.OnEditorDetach += delegate { window = false; };
			part.OnEditorDestroy += delegate { window = false; };
			part.OnJustAboutToBeDestroyed += delegate { window = false; };
		}
		else {
			_groups = new List<string>(groups.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
			print("Group: " + groups + " - Altitude: " + altitude + " - Delay: " + delay);
		}
	}
	
	public override void OnActive() {
		if (window) { return; }
		
		if (!triggered && FlightGlobals.fetch != null) {
			if (altitude > 0 && vessel.altitude < altitude) {
				armed = true;
				print("armed " + groups + " at " + altitude + "m");
			}
			else if (_groups != null && _groups.Count > 0) {
				foreach (string group in _groups)
				{
					if (Enum.IsDefined(typeof(KSPActionGroup), group)) {
						if (group == "Stage") {
							stage = true;
						}
						else {
							FlightGlobals.ActiveVessel.ActionGroups.ToggleGroup((KSPActionGroup)Enum.Parse(typeof(KSPActionGroup), group));
						}
					}
					else if (group == "Throttle Full") {
						FlightInputHandler.state.mainThrottle = 1;
					}
					else if (group == "Throttle None") {
						FlightInputHandler.state.mainThrottle = 0;
					}
					print(group);
				}
			}
		}
	}
	
	public override void OnFixedUpdate() {
		if (window) { return; }
		
		if (stage && !triggered) {
			Staging.ActivateNextStage();
			triggered = true;
			stage = false;
//			part.Die();
		}
		
		if (armed && vessel.altitude >= altitude) {
			armed = false;
			OnActive();
			print(groups + " at " + (int)vessel.altitude);
		}
	}
	
	private void windowGUI (int windowID)
	{
		GUIStyle style = new GUIStyle (GUI.skin.button);
		style.normal.textColor = style.focused.textColor = Color.white;
		style.hover.textColor = style.active.textColor = Color.yellow;
		style.onNormal.textColor = style.onFocused.textColor = style.onHover.textColor = style.onActive.textColor = Color.green;
		style.padding = new RectOffset (4, 4, 4, 4);
		
		GUIStyle activeStyle = new GUIStyle (GUI.skin.button);
		activeStyle.normal.textColor = activeStyle.focused.textColor = Color.green;
		activeStyle.hover.textColor = activeStyle.active.textColor = Color.yellow;
		activeStyle.onNormal.textColor = activeStyle.onFocused.textColor = activeStyle.onHover.textColor = activeStyle.onActive.textColor = Color.green;
		activeStyle.padding = new RectOffset (4, 4, 4, 4);

		GUILayout.BeginVertical ();
		
		var arr = (string.Join(",", KSPActionGroup.GetNames(typeof(KSPActionGroup))) + "," + string.Join(",", custom)).Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
		
		foreach (var e in arr) {
			if (e == "None") { continue; }
			if (GUILayout.Button(e, (_groups.Contains(e) ? activeStyle : style))) {
				if (_groups.Contains(e))
				{
					_groups.Remove(e);
				}
				else
				{
					_groups.Add(e);
				}
			}
		}
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Alt", GUILayout.ExpandWidth(true));
		altitude = int.Parse(GUILayout.TextField(altitude.ToString(), GUILayout.Width(60)));
		GUILayout.Label("m", GUILayout.ExpandWidth(false));
		GUILayout.EndHorizontal();
		
//		GUILayout.BeginHorizontal();
//		GUILayout.Label("Delay", GUILayout.ExpandWidth(true));
//		delay = float.Parse(GUILayout.TextField(delay.ToString(), GUILayout.Width(30)));
//		GUILayout.Label("s", GUILayout.ExpandWidth(false));
//		GUILayout.EndHorizontal();
		
		if (GUILayout.Button("Done")) {
			UpdateGroups();
			window = _groups.Count < 1;
			if (!window) {
				stage = armed = triggered = false;
			}
		}
		
		GUILayout.EndVertical ();

		GUI.DragWindow ();
	}
	
	private void drawGUI ()
	{
		if (!window || !part) { return; }
		
		winPosSize = GUILayout.Window(id, winPosSize, windowGUI, "Stage Action", GUILayout.MinWidth(120));
	}
	
	public void print(string msg) {
		Debug.Log("StageAction: " + msg);
	}
}