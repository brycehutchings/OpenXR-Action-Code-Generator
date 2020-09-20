#pragma once


struct GameplayActionSet
{
    ~GameplayActionSet()
    {
        if (ActionSet != XR_NULL_HANDLE)
        {
            (void)xrDestroyActionSet(ActionSet);
        }
    }

    XrResult Initialize(XrInstance instance)
    {
        XrActionSetCreateInfo actionSetInfo{XR_TYPE_ACTION_SET_CREATE_INFO};
        strcpy_s(actionSetInfo.actionSetName, "gameplay");
        strcpy_s(actionSetInfo.localizedActionSetName, "Gameplay");
        actionSetInfo.priority = 0;
        XrResult result = xrCreateActionSet(instance, &actionSetInfo, &ActionSet);

        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/left", &LeftHand);
        }

        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/right", &RightHand);
        }

        if (XR_SUCCEEDED(result))
        {
            const XrPath subactionPaths[] = { LeftHand, RightHand };
            XrActionCreateInfo actionCreateInfo{XR_TYPE_ACTION_CREATE_INFO};
            actionCreateInfo.actionType = XR_ACTION_TYPE_FLOAT_INPUT;
            strcpy_s(actionCreateInfo.actionName, "grab_object");
            strcpy_s(actionCreateInfo.localizedActionName, "Grab Object");
            actionCreateInfo.countSubactionPaths = 2;
            actionCreateInfo.subactionPaths = subactionPaths;
            result = xrCreateAction(ActionSet, &actionCreateInfo, &GrabObject);
        }

        if (XR_SUCCEEDED(result))
        {
            const XrPath subactionPaths[] = { LeftHand, RightHand };
            XrActionCreateInfo actionCreateInfo{XR_TYPE_ACTION_CREATE_INFO};
            actionCreateInfo.actionType = XR_ACTION_TYPE_POSE_INPUT;
            strcpy_s(actionCreateInfo.actionName, "hand_pose");
            strcpy_s(actionCreateInfo.localizedActionName, "Hand Pose");
            actionCreateInfo.countSubactionPaths = 2;
            actionCreateInfo.subactionPaths = subactionPaths;
            result = xrCreateAction(ActionSet, &actionCreateInfo, &HandPose);
        }

        if (XR_SUCCEEDED(result))
        {
            const XrPath subactionPaths[] = { LeftHand, RightHand };
            XrActionCreateInfo actionCreateInfo{XR_TYPE_ACTION_CREATE_INFO};
            actionCreateInfo.actionType = XR_ACTION_TYPE_POSE_INPUT;
            strcpy_s(actionCreateInfo.actionName, "hand_pose_both");
            strcpy_s(actionCreateInfo.localizedActionName, "Hand Pose Both");
            actionCreateInfo.countSubactionPaths = 2;
            actionCreateInfo.subactionPaths = subactionPaths;
            result = xrCreateAction(ActionSet, &actionCreateInfo, &HandPoseBoth);
        }

        if (XR_SUCCEEDED(result))
        {
            const XrPath subactionPaths[] = { LeftHand, RightHand };
            XrActionCreateInfo actionCreateInfo{XR_TYPE_ACTION_CREATE_INFO};
            actionCreateInfo.actionType = XR_ACTION_TYPE_VIBRATION_OUTPUT;
            strcpy_s(actionCreateInfo.actionName, "vibrate_hand");
            strcpy_s(actionCreateInfo.localizedActionName, "Vibrate Hand");
            actionCreateInfo.countSubactionPaths = 2;
            actionCreateInfo.subactionPaths = subactionPaths;
            result = xrCreateAction(ActionSet, &actionCreateInfo, &VibrateHand);
        }

        if (XR_SUCCEEDED(result))
        {
            const XrPath subactionPaths[] = { LeftHand, RightHand };
            XrActionCreateInfo actionCreateInfo{XR_TYPE_ACTION_CREATE_INFO};
            actionCreateInfo.actionType = XR_ACTION_TYPE_BOOLEAN_INPUT;
            strcpy_s(actionCreateInfo.actionName, "quit_session");
            strcpy_s(actionCreateInfo.localizedActionName, "Quit Session");
            actionCreateInfo.countSubactionPaths = 2;
            actionCreateInfo.subactionPaths = subactionPaths;
            result = xrCreateAction(ActionSet, &actionCreateInfo, &QuitSession);
        }

        return result;
    }

    XrResult CreateHandPoseLeftHandActionSpace(XrSession session, XrSpace* space) const
    {
        XrActionSpaceCreateInfo actionSpaceInfo{XR_TYPE_ACTION_SPACE_CREATE_INFO};
        actionSpaceInfo.action = HandPose;
        actionSpaceInfo.poseInActionSpace.orientation.w = 1.0f;
        actionSpaceInfo.subactionPath = LeftHand;
        return xrCreateActionSpace(session, &actionSpaceInfo, space);
    }

    XrResult CreateHandPoseRightHandActionSpace(XrSession session, XrSpace* space) const
    {
        XrActionSpaceCreateInfo actionSpaceInfo{XR_TYPE_ACTION_SPACE_CREATE_INFO};
        actionSpaceInfo.action = HandPose;
        actionSpaceInfo.poseInActionSpace.orientation.w = 1.0f;
        actionSpaceInfo.subactionPath = RightHand;
        return xrCreateActionSpace(session, &actionSpaceInfo, space);
    }

    XrResult CreateHandPoseBothActionSpace(XrSession session, XrSpace* space) const
    {
        XrActionSpaceCreateInfo actionSpaceInfo{XR_TYPE_ACTION_SPACE_CREATE_INFO};
        actionSpaceInfo.action = HandPoseBoth;
        actionSpaceInfo.poseInActionSpace.orientation.w = 1.0f;
        return xrCreateActionSpace(session, &actionSpaceInfo, space);
    }

    XrActionSet ActionSet{XR_NULL_HANDLE};

    XrAction GrabObject{XR_NULL_HANDLE};
    XrAction HandPose{XR_NULL_HANDLE};
    XrAction HandPoseBoth{XR_NULL_HANDLE};
    XrAction VibrateHand{XR_NULL_HANDLE};
    XrAction QuitSession{XR_NULL_HANDLE};

    XrPath LeftHand{XR_NULL_PATH};
    XrPath RightHand{XR_NULL_PATH};
};

struct GameplayActionStates
{
    struct SubactionStates
    {
        XrPath SubactionPath = XR_NULL_PATH;
        const XrActionStateFloat* GrabObjectActionState = nullptr;
        const XrActionStatePose* HandPoseActionState = nullptr;
    };

    XrResult UpdateActionStates(XrSession session, GameplayActionSet const& actionSet)
    {
        XrActionStateGetInfo actionStateGetInfo{XR_TYPE_ACTION_STATE_GET_INFO};
        XrResult result = XR_SUCCESS;

        if (XR_SUCCEEDED(result))
        {
            actionStateGetInfo.action = actionSet.GrabObject;
            actionStateGetInfo.subactionPath = actionSet.LeftHand;
            result = xrGetActionStateFloat(session, &actionStateGetInfo, &GrabObjectLeftHandActionState);
        }

        if (XR_SUCCEEDED(result))
        {
            actionStateGetInfo.action = actionSet.GrabObject;
            actionStateGetInfo.subactionPath = actionSet.RightHand;
            result = xrGetActionStateFloat(session, &actionStateGetInfo, &GrabObjectRightHandActionState);
        }

        if (XR_SUCCEEDED(result))
        {
            actionStateGetInfo.action = actionSet.HandPose;
            actionStateGetInfo.subactionPath = actionSet.LeftHand;
            result = xrGetActionStatePose(session, &actionStateGetInfo, &HandPoseLeftHandActionState);
        }

        if (XR_SUCCEEDED(result))
        {
            actionStateGetInfo.action = actionSet.HandPose;
            actionStateGetInfo.subactionPath = actionSet.RightHand;
            result = xrGetActionStatePose(session, &actionStateGetInfo, &HandPoseRightHandActionState);
        }

        if (XR_SUCCEEDED(result))
        {
            actionStateGetInfo.action = actionSet.HandPoseBoth;
            actionStateGetInfo.subactionPath = XR_NULL_PATH;
            result = xrGetActionStatePose(session, &actionStateGetInfo, &HandPoseBothActionState);
        }

        if (XR_SUCCEEDED(result))
        {
            actionStateGetInfo.action = actionSet.QuitSession;
            actionStateGetInfo.subactionPath = XR_NULL_PATH;
            result = xrGetActionStateBoolean(session, &actionStateGetInfo, &QuitSessionActionState);
        }
        return result;
    }

    SubactionStates GetSubactionStates(GameplayActionSet const& actionSet, XrPath subactionPath) const
    {
        if (subactionPath == actionSet.LeftHand)
        {
            return {subactionPath, &GrabObjectLeftHandActionState, &HandPoseLeftHandActionState};
        }
        else if (subactionPath == actionSet.RightHand)
        {
            return {subactionPath, &GrabObjectRightHandActionState, &HandPoseRightHandActionState};
        }
        else
        {
            return {}; // Unknown subaction path.
        }
    }

    XrActionStateFloat GrabObjectLeftHandActionState{XR_TYPE_ACTION_STATE_FLOAT};
    XrActionStateFloat GrabObjectRightHandActionState{XR_TYPE_ACTION_STATE_FLOAT};
    XrActionStatePose HandPoseLeftHandActionState{XR_TYPE_ACTION_STATE_POSE};
    XrActionStatePose HandPoseRightHandActionState{XR_TYPE_ACTION_STATE_POSE};
    XrActionStatePose HandPoseBothActionState{XR_TYPE_ACTION_STATE_POSE};
    XrActionStateBoolean QuitSessionActionState{XR_TYPE_ACTION_STATE_BOOLEAN};
};

struct SuggestedBindings
{
    XrResult Initialize(XrInstance instance, GameplayActionSet const& actionSet)
    {
        XrResult result = XR_SUCCESS;

        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/interaction_profiles/khr/simple_controller", &KhrSimpleController);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/interaction_profiles/oculus/touch_controller", &OculusTouchController);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/interaction_profiles/valve/index_controller", &ValveIndexController);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/interaction_profiles/microsoft/motion_controller", &MicrosoftMotionController);
        }

        XrPath UserHandLeftInputBClick = XR_NULL_PATH;
        XrPath UserHandLeftInputGripPose = XR_NULL_PATH;
        XrPath UserHandLeftInputMenuClick = XR_NULL_PATH;
        XrPath UserHandLeftInputSelectClick = XR_NULL_PATH;
        XrPath UserHandLeftInputSqueezeClick = XR_NULL_PATH;
        XrPath UserHandLeftInputSqueezeValue = XR_NULL_PATH;
        XrPath UserHandLeftInputTriggerValue = XR_NULL_PATH;
        XrPath UserHandLeftOutputHaptic = XR_NULL_PATH;
        XrPath UserHandRightInputBClick = XR_NULL_PATH;
        XrPath UserHandRightInputGripPose = XR_NULL_PATH;
        XrPath UserHandRightInputMenuClick = XR_NULL_PATH;
        XrPath UserHandRightInputSelectClick = XR_NULL_PATH;
        XrPath UserHandRightInputSqueezeClick = XR_NULL_PATH;
        XrPath UserHandRightInputSqueezeValue = XR_NULL_PATH;
        XrPath UserHandRightInputTriggerValue = XR_NULL_PATH;
        XrPath UserHandRightOutputHaptic = XR_NULL_PATH;
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/left/input/b/click", &UserHandLeftInputBClick);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/left/input/grip/pose", &UserHandLeftInputGripPose);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/left/input/menu/click", &UserHandLeftInputMenuClick);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/left/input/select/click", &UserHandLeftInputSelectClick);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/left/input/squeeze/click", &UserHandLeftInputSqueezeClick);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/left/input/squeeze/value", &UserHandLeftInputSqueezeValue);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/left/input/trigger/value", &UserHandLeftInputTriggerValue);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/left/output/haptic", &UserHandLeftOutputHaptic);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/right/input/b/click", &UserHandRightInputBClick);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/right/input/grip/pose", &UserHandRightInputGripPose);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/right/input/menu/click", &UserHandRightInputMenuClick);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/right/input/select/click", &UserHandRightInputSelectClick);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/right/input/squeeze/click", &UserHandRightInputSqueezeClick);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/right/input/squeeze/value", &UserHandRightInputSqueezeValue);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/right/input/trigger/value", &UserHandRightInputTriggerValue);
        }
        if (XR_SUCCEEDED(result))
        {
            result = xrStringToPath(instance, "/user/hand/right/output/haptic", &UserHandRightOutputHaptic);
        }

        if (XR_SUCCEEDED(result))
        {
            KhrSimpleControllerBindings[0] = {actionSet.GrabObject, UserHandLeftInputSelectClick};
            KhrSimpleControllerBindings[1] = {actionSet.GrabObject, UserHandRightInputSelectClick};
            KhrSimpleControllerBindings[2] = {actionSet.HandPose, UserHandLeftInputGripPose};
            KhrSimpleControllerBindings[3] = {actionSet.HandPose, UserHandRightInputGripPose};
            KhrSimpleControllerBindings[4] = {actionSet.HandPoseBoth, UserHandLeftInputGripPose};
            KhrSimpleControllerBindings[5] = {actionSet.HandPoseBoth, UserHandRightInputGripPose};
            KhrSimpleControllerBindings[6] = {actionSet.VibrateHand, UserHandLeftOutputHaptic};
            KhrSimpleControllerBindings[7] = {actionSet.VibrateHand, UserHandRightOutputHaptic};
            KhrSimpleControllerBindings[8] = {actionSet.QuitSession, UserHandLeftInputMenuClick};
            KhrSimpleControllerBindings[9] = {actionSet.QuitSession, UserHandRightInputMenuClick};
            OculusTouchControllerBindings[0] = {actionSet.GrabObject, UserHandLeftInputSqueezeValue};
            OculusTouchControllerBindings[1] = {actionSet.GrabObject, UserHandRightInputSqueezeValue};
            OculusTouchControllerBindings[2] = {actionSet.HandPose, UserHandLeftInputGripPose};
            OculusTouchControllerBindings[3] = {actionSet.HandPose, UserHandRightInputGripPose};
            OculusTouchControllerBindings[4] = {actionSet.HandPoseBoth, UserHandLeftInputGripPose};
            OculusTouchControllerBindings[5] = {actionSet.HandPoseBoth, UserHandRightInputGripPose};
            OculusTouchControllerBindings[6] = {actionSet.VibrateHand, UserHandLeftOutputHaptic};
            OculusTouchControllerBindings[7] = {actionSet.VibrateHand, UserHandRightOutputHaptic};
            OculusTouchControllerBindings[8] = {actionSet.QuitSession, UserHandLeftInputMenuClick};
            ValveIndexControllerBindings[0] = {actionSet.GrabObject, UserHandLeftInputTriggerValue};
            ValveIndexControllerBindings[1] = {actionSet.GrabObject, UserHandRightInputTriggerValue};
            ValveIndexControllerBindings[2] = {actionSet.HandPose, UserHandLeftInputGripPose};
            ValveIndexControllerBindings[3] = {actionSet.HandPose, UserHandRightInputGripPose};
            ValveIndexControllerBindings[4] = {actionSet.HandPoseBoth, UserHandLeftInputGripPose};
            ValveIndexControllerBindings[5] = {actionSet.HandPoseBoth, UserHandRightInputGripPose};
            ValveIndexControllerBindings[6] = {actionSet.VibrateHand, UserHandLeftOutputHaptic};
            ValveIndexControllerBindings[7] = {actionSet.VibrateHand, UserHandRightOutputHaptic};
            ValveIndexControllerBindings[8] = {actionSet.QuitSession, UserHandLeftInputBClick};
            ValveIndexControllerBindings[9] = {actionSet.QuitSession, UserHandRightInputBClick};
            MicrosoftMotionControllerBindings[0] = {actionSet.GrabObject, UserHandLeftInputSqueezeClick};
            MicrosoftMotionControllerBindings[1] = {actionSet.GrabObject, UserHandRightInputSqueezeClick};
            MicrosoftMotionControllerBindings[2] = {actionSet.HandPose, UserHandLeftInputGripPose};
            MicrosoftMotionControllerBindings[3] = {actionSet.HandPose, UserHandRightInputGripPose};
            MicrosoftMotionControllerBindings[4] = {actionSet.HandPoseBoth, UserHandLeftInputGripPose};
            MicrosoftMotionControllerBindings[5] = {actionSet.HandPoseBoth, UserHandRightInputGripPose};
            MicrosoftMotionControllerBindings[6] = {actionSet.VibrateHand, UserHandLeftOutputHaptic};
            MicrosoftMotionControllerBindings[7] = {actionSet.VibrateHand, UserHandRightOutputHaptic};
            MicrosoftMotionControllerBindings[8] = {actionSet.QuitSession, UserHandLeftInputMenuClick};
            MicrosoftMotionControllerBindings[9] = {actionSet.QuitSession, UserHandRightInputMenuClick};
        }

        return result;
    }

    XrResult SuggestInteractionProfileBindings(XrInstance instance)
    {
        XrResult result = XR_SUCCESS;

        if (XR_SUCCEEDED(result))
        {
            XrInteractionProfileSuggestedBinding interactionSuggestedBindings{XR_TYPE_INTERACTION_PROFILE_SUGGESTED_BINDING};
            interactionSuggestedBindings.interactionProfile = KhrSimpleController;
            interactionSuggestedBindings.suggestedBindings = KhrSimpleControllerBindings;
            interactionSuggestedBindings.countSuggestedBindings = 10;
            result = xrSuggestInteractionProfileBindings(instance, &interactionSuggestedBindings);
        }

        if (XR_SUCCEEDED(result))
        {
            XrInteractionProfileSuggestedBinding interactionSuggestedBindings{XR_TYPE_INTERACTION_PROFILE_SUGGESTED_BINDING};
            interactionSuggestedBindings.interactionProfile = OculusTouchController;
            interactionSuggestedBindings.suggestedBindings = OculusTouchControllerBindings;
            interactionSuggestedBindings.countSuggestedBindings = 9;
            result = xrSuggestInteractionProfileBindings(instance, &interactionSuggestedBindings);
        }

        if (XR_SUCCEEDED(result))
        {
            XrInteractionProfileSuggestedBinding interactionSuggestedBindings{XR_TYPE_INTERACTION_PROFILE_SUGGESTED_BINDING};
            interactionSuggestedBindings.interactionProfile = ValveIndexController;
            interactionSuggestedBindings.suggestedBindings = ValveIndexControllerBindings;
            interactionSuggestedBindings.countSuggestedBindings = 10;
            result = xrSuggestInteractionProfileBindings(instance, &interactionSuggestedBindings);
        }

        if (XR_SUCCEEDED(result))
        {
            XrInteractionProfileSuggestedBinding interactionSuggestedBindings{XR_TYPE_INTERACTION_PROFILE_SUGGESTED_BINDING};
            interactionSuggestedBindings.interactionProfile = MicrosoftMotionController;
            interactionSuggestedBindings.suggestedBindings = MicrosoftMotionControllerBindings;
            interactionSuggestedBindings.countSuggestedBindings = 10;
            result = xrSuggestInteractionProfileBindings(instance, &interactionSuggestedBindings);
        }

        return result;
    }

    XrPath KhrSimpleController;
    XrPath OculusTouchController;
    XrPath ValveIndexController;
    XrPath MicrosoftMotionController;

    XrActionSuggestedBinding KhrSimpleControllerBindings[10];
    XrActionSuggestedBinding OculusTouchControllerBindings[9];
    XrActionSuggestedBinding ValveIndexControllerBindings[10];
    XrActionSuggestedBinding MicrosoftMotionControllerBindings[10];
};
