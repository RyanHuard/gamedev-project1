using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HollowTournament.EditorTools
{
    /// <summary>
    /// Builds simple procedural Animator Controllers + AnimationClips so the
    /// prototype has real (if crude) animation without needing Mixamo imports.
    /// Clips animate a child Transform named "Model" so they work on plain
    /// primitive characters; swapping in a rigged humanoid later just means
    /// replacing these clips, not the controller wiring.
    /// </summary>
    public static class AnimatorControllerBuilder
    {
        private const string ClipFolder = "Assets/Animations";
        private const string ControllerFolder = "Assets/Animations";
        private const string ModelPath = "Model";

        public static AnimatorController BuildCombatantController()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath($"{ControllerFolder}/CombatantController.controller");
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Blocking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("WindUp", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Release", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Riposte", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Kick", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Staggered", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("SwingDirection", AnimatorControllerParameterType.Int);

            var sm = controller.layers[0].stateMachine;

            var idle = sm.AddState("Idle");
            idle.motion = CreateBobClip("Idle_Bob", 0.03f, 1.2f);
            sm.defaultState = idle;

            var move = sm.AddState("Move");
            move.motion = CreateBobClip("Move_Bob", 0.08f, 3.5f);

            AddBoolToggle(idle, move, "Moving");

            var block = sm.AddState("Block");
            block.motion = CreateLeanClip("Block_Lean", -15f, 0.15f, true);
            AddBoolToggle(idle, block, "Blocking");

            var windUp = sm.AddState("WindUp");
            windUp.motion = CreateLeanClip("WindUp_Lean", 20f, 0.35f, false);
            AddAnyStateTrigger(sm, windUp, idle, "WindUp");

            var attack = sm.AddState("Attack");
            attack.motion = CreateSwingClip("Attack_Swing", -70f, 0.25f);
            AddAnyStateTrigger(sm, attack, idle, "Release");
            AddAnyStateTrigger(sm, attack, idle, "Attack");

            var riposte = sm.AddState("Riposte");
            riposte.motion = CreateSwingClip("Riposte_Swing", -100f, 0.2f);
            AddAnyStateTrigger(sm, riposte, idle, "Riposte");

            var kick = sm.AddState("Kick");
            kick.motion = CreateLungeClip("Kick_Lunge", 0.4f, 0.25f);
            AddAnyStateTrigger(sm, kick, idle, "Kick");

            var stagger = sm.AddState("Stagger");
            stagger.motion = CreateShakeClip("Stagger_Shake", 10f, 0.5f);
            AddAnyStateTrigger(sm, stagger, idle, "Staggered");

            var dead = sm.AddState("Dead");
            dead.motion = CreateFallClip("Dead_Fall");
            var toDead = sm.AddAnyStateTransition(dead);
            toDead.hasExitTime = false;
            toDead.duration = 0.1f;
            toDead.AddCondition(AnimatorConditionMode.If, 0, "Dead");

            return controller;
        }

        public static AnimatorController BuildHorsemanController()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath($"{ControllerFolder}/HorsemanController.controller");
            controller.AddParameter("Charge", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Turn", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Unhorsed", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("WindUp", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Feint", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Blocking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Staggered", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("HeadThrowWindUp", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Defeated", AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;

            var idle = sm.AddState("Idle");
            idle.motion = CreateBobClip("Horseman_Idle_Bob", 0.03f, 1f);
            sm.defaultState = idle;

            var charge = sm.AddState("Charge");
            charge.motion = CreateBobClip("Horseman_Charge_Bob", 0.1f, 6f);
            AddAnyStateTrigger(sm, charge, idle, "Charge");

            var turn = sm.AddState("Turn");
            turn.motion = CreateSwingClip("Horseman_Turn", 180f, 1.0f);
            AddAnyStateTrigger(sm, turn, idle, "Turn");

            var unhorsed = sm.AddState("Unhorsed");
            unhorsed.motion = CreateFallClip("Horseman_Unhorsed_Fall");
            AddAnyStateTrigger(sm, unhorsed, idle, "Unhorsed");

            var windUp = sm.AddState("WindUp");
            windUp.motion = CreateLeanClip("Horseman_WindUp_Lean", 20f, 0.4f, false);
            AddAnyStateTrigger(sm, windUp, idle, "WindUp");

            var attack = sm.AddState("Attack");
            attack.motion = CreateSwingClip("Horseman_Attack_Swing", -70f, 0.25f);
            AddAnyStateTrigger(sm, attack, idle, "Attack");

            var feint = sm.AddState("Feint");
            feint.motion = CreateShakeClip("Horseman_Feint_Shake", 6f, 0.3f);
            AddAnyStateTrigger(sm, feint, idle, "Feint");

            var block = sm.AddState("Block");
            block.motion = CreateLeanClip("Horseman_Block_Lean", -15f, 0.15f, true);
            AddBoolToggle(idle, block, "Blocking");

            var stagger = sm.AddState("Stagger");
            stagger.motion = CreateShakeClip("Horseman_Stagger_Shake", 10f, 0.5f);
            AddAnyStateTrigger(sm, stagger, idle, "Staggered");

            var headThrow = sm.AddState("HeadThrow");
            headThrow.motion = CreateLungeClip("Horseman_HeadThrow_Lunge", 0.3f, 0.6f);
            AddAnyStateTrigger(sm, headThrow, idle, "HeadThrowWindUp");

            var defeated = sm.AddState("Defeated");
            defeated.motion = CreateFallClip("Horseman_Defeated_Fall");
            var toDefeated = sm.AddAnyStateTransition(defeated);
            toDefeated.hasExitTime = false;
            toDefeated.duration = 0.1f;
            toDefeated.AddCondition(AnimatorConditionMode.If, 0, "Defeated");

            return controller;
        }

        private static void AddBoolToggle(AnimatorState a, AnimatorState b, string boolParam)
        {
            var toB = a.AddTransition(b);
            toB.hasExitTime = false;
            toB.duration = 0.1f;
            toB.AddCondition(AnimatorConditionMode.If, 0, boolParam);

            var toA = b.AddTransition(a);
            toA.hasExitTime = false;
            toA.duration = 0.1f;
            toA.AddCondition(AnimatorConditionMode.IfNot, 0, boolParam);
        }

        private static void AddAnyStateTrigger(AnimatorStateMachine sm, AnimatorState target, AnimatorState returnTo, string triggerParam)
        {
            var toTarget = sm.AddAnyStateTransition(target);
            toTarget.hasExitTime = false;
            toTarget.duration = 0.05f;
            toTarget.AddCondition(AnimatorConditionMode.If, 0, triggerParam);

            var toReturn = target.AddTransition(returnTo);
            toReturn.hasExitTime = true;
            toReturn.exitTime = 1f;
            toReturn.duration = 0.1f;
        }

        public static AnimatorController BuildGateController()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath($"{ControllerFolder}/GateController.controller");
            controller.AddParameter("Open", AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;
            var closed = sm.AddState("Closed");
            sm.defaultState = closed;

            var open = sm.AddState("Open");
            open.motion = CreateGateRiseClip("Gate_Rise");

            var toOpen = sm.AddAnyStateTransition(open);
            toOpen.hasExitTime = false;
            toOpen.duration = 0.1f;
            toOpen.AddCondition(AnimatorConditionMode.If, 0, "Open");

            return controller;
        }

        private static AnimationClip CreateGateRiseClip(string name)
        {
            var clip = NewClip(name, false);
            var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 3f));
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.y");
            AnimationUtility.SetEditorCurve(clip, binding, curve);
            return SaveClip(clip, name);
        }

        // ---------- Clip factories (all animate the "Model" child's local transform) ----------

        private static AnimationClip CreateBobClip(string name, float amplitude, float cyclesPerSecond)
        {
            var clip = NewClip(name, true);
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f / cyclesPerSecond, amplitude),
                new Keyframe(1f / cyclesPerSecond, 0f));
            SetCurve(clip, "localPosition.y", curve);
            return SaveClip(clip, name);
        }

        private static AnimationClip CreateLeanClip(string name, float angleX, float duration, bool loop)
        {
            var clip = NewClip(name, loop);
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(duration * 0.5f, angleX),
                new Keyframe(duration, loop ? angleX : 0f));
            SetCurve(clip, "localEulerAnglesRaw.x", curve);
            return SaveClip(clip, name);
        }

        private static AnimationClip CreateSwingClip(string name, float angleY, float duration)
        {
            var clip = NewClip(name, false);
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(duration * 0.6f, angleY),
                new Keyframe(duration, 0f));
            SetCurve(clip, "localEulerAnglesRaw.y", curve);
            return SaveClip(clip, name);
        }

        private static AnimationClip CreateShakeClip(string name, float angle, float duration)
        {
            var clip = NewClip(name, false);
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(duration * 0.2f, angle),
                new Keyframe(duration * 0.4f, -angle),
                new Keyframe(duration * 0.6f, angle * 0.6f),
                new Keyframe(duration * 0.8f, -angle * 0.3f),
                new Keyframe(duration, 0f));
            SetCurve(clip, "localEulerAnglesRaw.z", curve);
            return SaveClip(clip, name);
        }

        private static AnimationClip CreateLungeClip(string name, float distance, float duration)
        {
            var clip = NewClip(name, false);
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(duration * 0.4f, distance),
                new Keyframe(duration, 0f));
            SetCurve(clip, "localPosition.z", curve);
            return SaveClip(clip, name);
        }

        private static AnimationClip CreateFallClip(string name)
        {
            var clip = NewClip(name, false);
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 85f));
            SetCurve(clip, "localEulerAnglesRaw.x", curve);
            return SaveClip(clip, name);
        }

        private static AnimationClip NewClip(string name, bool loop)
        {
            var clip = new AnimationClip { name = name };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static void SetCurve(AnimationClip clip, string property, AnimationCurve curve)
        {
            var binding = EditorCurveBinding.FloatCurve(ModelPath, typeof(Transform), property);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static AnimationClip SaveClip(AnimationClip clip, string name)
        {
            string path = $"{ClipFolder}/{name}.anim";
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }
    }
}
