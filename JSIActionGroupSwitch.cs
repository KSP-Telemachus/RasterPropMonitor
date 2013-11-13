using System;
using System.Collections.Generic;
using UnityEngine;

namespace JSI
{
	public class JSIActionGroupSwitch: InternalModule
	{
		[KSPField]
		public string animationName = "";
		[KSPField]
		public string switchTransform = "";
		[KSPField]
		public string actionName = "lights";
		[KSPField]
		public bool reverse = false;
		[KSPField]
		public float customSpeed = 1f;
		[KSPField]
		public string internalLightName = null;
		// Neater.
		private Dictionary<string,KSPActionGroup> groupList = new Dictionary<string,KSPActionGroup> { 
			{ "gear",KSPActionGroup.Gear },
			{ "brakes",KSPActionGroup.Brakes },
			{ "lights",KSPActionGroup.Light },
			{ "rcs",KSPActionGroup.RCS },
			{ "sas",KSPActionGroup.SAS },
			{ "abort",KSPActionGroup.Abort },
			{ "stage",KSPActionGroup.Stage },
			{ "custom01",KSPActionGroup.Custom01 },
			{ "custom02",KSPActionGroup.Custom02 },
			{ "custom03",KSPActionGroup.Custom03 },
			{ "custom04",KSPActionGroup.Custom04 },
			{ "custom05",KSPActionGroup.Custom05 },
			{ "custom06",KSPActionGroup.Custom06 },
			{ "custom07",KSPActionGroup.Custom07 },
			{ "custom08",KSPActionGroup.Custom08 },
			{ "custom09",KSPActionGroup.Custom09 },
			{ "custom10",KSPActionGroup.Custom10 }
		};

		private Dictionary<string,bool> customGroupList = new Dictionary<string,bool> {
			{ "intlight",false },
			{ "dummy",false }
		};
		private int actionGroupID;
		private KSPActionGroup actionGroup;
		private Animation anim;
		private bool oldstate = false;
		private bool iscustomaction = false;
		// Persistence for current state variable.
		private PersistenceAccessor persistence;
		private string persistentVarName;
		private Light[] lightobjects;

		private static void LogMessage(string line, params object[] list)
		{
			Debug.Log(String.Format(typeof(JSIActionGroupSwitch).Name + ": " + line, list));
		}

		public void Start()
		{
			if (!groupList.ContainsKey(actionName)) {
				if (!customGroupList.ContainsKey(actionName)) {
					LogMessage("Action \"{0}\" not known, the switch will not work correctly.", actionName);
				} else {
					iscustomaction = true;
				}
			} else {
				actionGroup = groupList[actionName];
				actionGroupID = BaseAction.GetGroupIndex(actionGroup);

				oldstate = FlightGlobals.ActiveVessel.ActionGroups.groups[actionGroupID];
			}

			// Load our state from storage...
			if (iscustomaction) {
				if (actionName == "intlight")
					persistentVarName = internalLightName;
				else
					persistentVarName = "switch" + internalProp.propID.ToString();

				persistence = new PersistenceAccessor(part);

				oldstate = customGroupList[actionName] = (persistence.GetBool(persistentVarName) ?? oldstate);

			}

			// set up the toggle switch
			SmarterButton.CreateButton(internalProp, switchTransform, Click);

			// Set up the animation
			anim = internalProp.FindModelAnimators(animationName)[0];
			if (anim != null) {
				anim[animationName].wrapMode = WrapMode.Once;

			} else {
				LogMessage("Animation \"{0}\" not found, the switch will not work correctly.", animationName);
			}

			if (oldstate ^ reverse) {
				anim[animationName].speed = float.MaxValue;
				anim[animationName].normalizedTime = 0;

			} else {

				anim[animationName].speed = float.MinValue;
				anim[animationName].normalizedTime = 1;

			}
			anim.Play(animationName);

			// Set up the custom actions..
			switch (actionName) {
				case "intlight":
					lightobjects = internalModel.FindModelComponents<Light>();
					SetInternalLights(customGroupList[actionName]);
					break;
				default:
					break;
			}

		}

		private void SetInternalLights(bool value)
		{
			foreach (Light lightobject in lightobjects) {
				// I probably shouldn't filter them every time, but I am getting
				// serously confused by this hierarchy.
				if (lightobject.name == internalLightName)
					lightobject.enabled = value;
			}
		}

		public void Click()
		{
			if (iscustomaction) {
				customGroupList[actionName] = !customGroupList[actionName];
				switch (actionName) {
					case "intlight":
						SetInternalLights(customGroupList[actionName]);
						break;
					default:
						break;
				}
				persistence.SetVar(persistentVarName, customGroupList[actionName]);
			} else
				FlightGlobals.ActiveVessel.ActionGroups.ToggleGroup(actionGroup);
		}

		public override void OnUpdate()
		{
			if (!HighLogic.LoadedSceneIsFlight ||
			    vessel != FlightGlobals.ActiveVessel)
				return;

			// Bizarre, but looks like I need to animate things offscreen if I want them in the right condition when camera comes back.
			/*&&
			    (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA ||
			    CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal)
			    ))
				return;*/

			bool state;
			if (iscustomaction) {
				state = customGroupList[actionName];
			} else {
				state = FlightGlobals.ActiveVessel.ActionGroups.groups[actionGroupID];
			}

			if (state != oldstate) {
				if (state ^ reverse) {
					anim[animationName].normalizedTime = 0;
					anim[animationName].speed = 1f * customSpeed;
					anim.Play(animationName);
				} else {
					anim[animationName].normalizedTime = 1;
					anim[animationName].speed = -1f * customSpeed;
					anim.Play(animationName);
				}
				oldstate = state;
			}
		}
	}


}

