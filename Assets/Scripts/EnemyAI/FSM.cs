/************************************************************************
 * Written by Nicholas Mirchandani on 4/25/2021                         *
 *                                                                      *
 * The purpose of FSM.cs is to provide the abstraction of a DFA to be   *
 * used to implement an enemy state machine.  The code intentionally is *
 * simplified, choosing to use unsigned ints to store the states so     *
 * enums can be used to ensure easily readable code in implementation   *
 *                                                                      *
 ************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public class FSM {
    [System.Serializable] class Transition {
        public uint startState;
        public uint endState;
        public uint action; //NOTE: Don't love the use of "action" here; it reflects the identifier for the "stimuli" that can cause an enemy to transition to a different state.

        public Transition(uint startState, uint endState, uint action) {
            this.startState = startState;
            this.endState = endState;
            this.action = action;
        }
    }

    public uint currentState;
    List<Transition> transitions;

    //Default Constructor: when creating an FSM, you give it an initial state.  Adding transitions is a separate method call for readability.
    public FSM(uint initialState) {
        currentState = initialState;
        transitions = new List<Transition>();
    }

    public void addTransition(uint startState, uint endState, uint action) {
        transitions.Add(new Transition(startState, endState, action));
    }

    public void applyTransition(uint action) {
        //NOTE: Assumes there is only one valid match within the List<Transition>, so returns first match.  If no match, nothing happens
        Transition transitionToApply = transitions.Find(x => x.startState == currentState && x.action == action);
        if (transitionToApply != null) {
            currentState = transitionToApply.endState;
        }
    }
}
