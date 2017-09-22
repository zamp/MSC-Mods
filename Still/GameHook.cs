using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

/* Usage:
// save game event
GameHook.InjectStateHook(GameObject.Find("ITEMS"), "Save game", Save);
*/


namespace MSCStill
{
	public class GameHook
	{
		private class FsmHookAction : FsmStateAction
		{
			public Action hook;
			public override void OnEnter()
			{
				hook.Invoke();
				Finish();
			}
		}

		public static void InjectStateHook(GameObject gameObject, string stateName, FsmStateAction customStateAction, int index = 0)
		{
			var state = GetStateFromGameObject(gameObject, stateName);
			if (state != null)
			{
				// inject our hook action to the state machine
				var actions = new List<FsmStateAction>(state.Actions);
				actions.Insert(index, customStateAction);
				state.Actions = actions.ToArray();
			}
		}

		public static void InjectStateHook(GameObject gameObject, string stateName, Action hook, int index = 0)
		{
			var state = GetStateFromGameObject(gameObject, stateName);
			if (state != null)
			{
				// inject our hook action to the state machine
				var actions = new List<FsmStateAction>(state.Actions);
				var hookAction = new FsmHookAction();
				hookAction.hook = hook;
				actions.Insert(index, hookAction);
				state.Actions = actions.ToArray();
			}
		}

		private static FsmState GetStateFromGameObject(GameObject obj, string stateName)
		{
			var comps = obj.GetComponents<PlayMakerFSM>();
			foreach (var playMakerFsm in comps)
			{
				var state = playMakerFsm.FsmStates.FirstOrDefault(x => x.Name == stateName);
				if (state != null)
					return state;
			}
			return null;
		}
	}
}
