using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using Satchel;

namespace CompleteP5;

public static class FSMStateExtensions
{
    public static void RemoveFirstAction<T>(this PlayMakerFSM fsm, string stateName) where T : FsmStateAction
    {
        FsmState state = fsm.GetState(stateName);
        List<FsmStateAction> newActions = state.Actions.ToList();
        
        try
        {
            int index = newActions.IndexOf(newActions.First(action => action is T));
            newActions.RemoveAt(index);
        }
        catch (Exception e)
        {
            CompleteP5.Instance.LogError("Action not found");
        }

        
        state.Actions = newActions.ToArray();
    }
}
