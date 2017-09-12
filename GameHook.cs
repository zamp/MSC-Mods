using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace CHANGEME
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

		public static void InjectStateHook(GameObject gameObject, string stateName, Action hook)
		{
			var state = GetStateFromGameObject(gameObject, stateName);
			if (state != null)
			{
				// inject our hook action to the state machine
				var actions = new List<FsmStateAction>(state.Actions);
				var hookAction = new FsmHookAction();
				hookAction.hook = hook;
				actions.Insert(0, hookAction);
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
