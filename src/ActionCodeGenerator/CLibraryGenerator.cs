using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenXRActionCodeGenerator
{
    class CLibraryGenerator
    {
        public static string Generate(ActionManifest actionManifest, INamingConventionConverter conv)
        {
            var sb = new StringBuilder();
            var indent = 0;

            Action<string> appendLineTrim = (string text) => sb.AppendLine(text.TrimEnd());
            Action<string> appendLine = (string text) => appendLineTrim($"{new string(' ', indent * 4)}{text}");
            Action<bool> addLineIfTrue = (bool condition) => { if (condition) { appendLineTrim(""); } };

            appendLine("#pragma once"); // TODO: Use C style

            appendLine("");

            foreach (var actionSet in actionManifest.ActionSets)
            {
                WriteActionSetStruct(sb, actionSet, indent, conv);
                WriteActionSetStateStruct(sb, actionSet, indent, conv);
                WriteSuggestedBindingsStruct(sb, actionSet, indent, conv);
            }

            return sb.ToString();
        }

        private static void WriteActionSetInitializeMethod(StringBuilder sb, ActionSet actionSet, int indent, INamingConventionConverter conv)
        {
            Action<string> appendLineTrim = (string text) => sb.AppendLine(text.TrimEnd());
            Action<string> appendLine = (string text) => appendLineTrim($"{new string(' ', indent * 4)}{text}");
            Action<bool> addLineIfTrue = (bool condition) => { if (condition) { appendLineTrim(""); } };

            appendLine("XrResult Initialize(XrInstance instance)");
            appendLine("{");
            indent++;

            // TODO: Add condition to return error if already inintialized.

            appendLine($"XrActionSetCreateInfo actionSetInfo{{XR_TYPE_ACTION_SET_CREATE_INFO}};");
            appendLine($"strcpy_s(actionSetInfo.actionSetName, \"{actionSet.Name}\");");
            appendLine($"strcpy_s(actionSetInfo.localizedActionSetName, \"{actionSet.LocalizedName}\");");
            appendLine($"actionSetInfo.priority = {actionSet.Priority};");
            appendLine($"XrResult result = xrCreateActionSet(instance, &actionSetInfo, &ActionSet);");

            var allSubactionPathInActionSet = actionSet.Actions.SelectMany(a => GetActionSubactionPaths(actionSet, a)).ToHashSet();
            foreach (var subactionPath in allSubactionPathInActionSet)
            {
                if (subactionPath == TopLevelPath.Null)
                {
                    continue;
                }

                appendLine("");
                appendLine("if (XR_SUCCEEDED(result))");
                appendLine("{");
                indent++;
                appendLine($"result = xrStringToPath(instance, \"{GetSubactionPath(subactionPath)}\", &{GetSubactionMemberName(subactionPath, conv)});");
                indent--;
                appendLine("}");
            }

            foreach (Action action in actionSet.Actions)
            {
                appendLine("");
                appendLine("if (XR_SUCCEEDED(result))");
                appendLine("{");
                indent++;

                var subactionPaths = GetActionSubactionPaths(actionSet, action);
                if (subactionPaths.Any())
                {
                    appendLine($"const XrPath subactionPaths[] = {{ {string.Join(", ", subactionPaths.Select(s => GetSubactionMemberName(s, conv)))} }};");
                }

                appendLine($"XrActionCreateInfo actionCreateInfo{{XR_TYPE_ACTION_CREATE_INFO}};");
                appendLine($"actionCreateInfo.actionType = {GetActionType(action.Type)};");
                appendLine($"strcpy_s(actionCreateInfo.actionName, \"{action.Name}\");");
                appendLine($"strcpy_s(actionCreateInfo.localizedActionName, \"{action.LocalizedName}\");");

                if (subactionPaths.Any())
                {
                    appendLine($"actionCreateInfo.countSubactionPaths = {subactionPaths.Count()};");
                    appendLine($"actionCreateInfo.subactionPaths = subactionPaths;");
                }
                appendLine($"result = xrCreateAction(ActionSet, &actionCreateInfo, &{GetActionHandleMemberName(action.Name, conv)});");
                indent--;
                appendLine("}");
            }

            appendLine("");
            appendLine("return result;");
            indent--;
            appendLine("}");
        }

        private static void WriteCreateActionSpaceHelpers(StringBuilder sb, ActionSet actionSet, Action action, int indent, INamingConventionConverter conv)
        {
            Action<string> appendLineTrim = (string text) => sb.AppendLine(text.TrimEnd());
            Action<string> appendLine = (string text) => appendLineTrim($"{new string(' ', indent * 4)}{text}");
            Action<bool> addLineIfTrue = (bool condition) => { if (condition) { appendLineTrim(""); } };

            Action<TopLevelPath> writeActionSpaceFunction = (TopLevelPath subactionPath) =>
            {
                appendLine("");
                appendLine($"XrResult {conv.Rename("Create", GetNameWithSubaction(action.Name, subactionPath), "ActionSpace")}(XrSession session, XrSpace* space) const");
                appendLine("{");
                indent++;

                appendLine($"XrActionSpaceCreateInfo actionSpaceInfo{{XR_TYPE_ACTION_SPACE_CREATE_INFO}};");
                appendLine($"actionSpaceInfo.action = {GetActionHandleMemberName(action.Name, conv)};");
                appendLine($"actionSpaceInfo.poseInActionSpace.orientation.w = 1.0f;");

                if (subactionPath != TopLevelPath.Null)
                {
                    appendLine($"actionSpaceInfo.subactionPath = {GetSubactionMemberName(subactionPath, conv)};");
                }

                appendLine($"return xrCreateActionSpace(session, &actionSpaceInfo, space);");

                indent--;
                appendLine("}");
            };

            foreach (var subaction in GetActionSubactionPaths(actionSet, action, includeNull: true))
            {
                writeActionSpaceFunction(subaction);
            }
        }

        private static void WriteActionSetStruct(StringBuilder sb, ActionSet actionSet, int indent, INamingConventionConverter conv)
        {
            Action<string> appendLineTrim = (string text) => sb.AppendLine(text.TrimEnd());
            Action<string> appendLine = (string text) => appendLineTrim($"{new string(' ', indent * 4)}{text}");
            Action<bool> addLineIfTrue = (bool condition) => { if (condition) { appendLineTrim(""); } };

            appendLine("");
            appendLine($"struct {GetActionSetStructName(actionSet, conv)}");
            appendLine("{");
            indent++;

            WriteActionSetInitializeMethod(sb, actionSet, indent, conv);

            foreach (var action in actionSet.Actions)
            {
                if (action.Type != ActionType.Pose)
                {
                    continue;
                }

                WriteCreateActionSpaceHelpers(sb, actionSet, action, indent, conv);
            }

            appendLine("");
            appendLine($"XrActionSet {conv.Rename("ActionSet")}{{XR_NULL_HANDLE}};");

            appendLine("");
            foreach (var action in actionSet.Actions)
            {
                appendLine($"XrAction {GetActionHandleMemberName(action.Name, conv)}{{XR_NULL_HANDLE}};");
            }

            // Write out the used subaction paths.
            var usedTopLevelPathsWithoutNull = actionSet.Actions.SelectMany(a => GetActionSubactionPaths(actionSet, a)).ToHashSet();
            addLineIfTrue(usedTopLevelPathsWithoutNull.Any());
            foreach (var subactionPath in usedTopLevelPathsWithoutNull)
            {
                appendLine($"XrPath {GetSubactionMemberName(subactionPath, conv)}{{XR_NULL_PATH}};");
            }

            indent--;
            appendLine("};");
        }

        private static void WriteActionSetStateStruct(StringBuilder sb, ActionSet actionSet, int indent, INamingConventionConverter conv)
        {
            Action<string> appendLineTrim = (string text) => sb.AppendLine(text.TrimEnd());
            Action<string> appendLine = (string text) => appendLineTrim($"{new string(' ', indent * 4)}{text}");
            Action<bool> addLineIfTrue = (bool condition) => { if (condition) { appendLineTrim(""); } };

            appendLine("");
            appendLine($"struct {conv.Rename("", actionSet.Name, "ActionStates")}");
            appendLine("{");
            indent++;

            appendLine($"XrResult UpdateActionStates(XrSession session, {GetActionSetStructName(actionSet, conv)} const& actionSet)");
            appendLine("{");
            indent++;
            appendLine("XrActionStateGetInfo actionStateGetInfo{XR_TYPE_ACTION_STATE_GET_INFO};");
            appendLine("XrResult result = XR_SUCCESS;");
            foreach (var action in actionSet.Actions)
            {
                if (action.Type == ActionType.Haptic)
                {
                    continue; // Skip out actions.
                }

                foreach (var subaction in GetActionSubactionPaths(actionSet, action, includeNull: true))
                {
                    appendLine("");

                    appendLine("if (XR_SUCCEEDED(result))");
                    appendLine("{");
                    indent++;
                    appendLine($"actionStateGetInfo.action = actionSet.{GetActionHandleMemberName(action.Name, conv)};");

                    if (action.UseSubactionPaths)
                    {
                        appendLine($"actionStateGetInfo.subactionPath = actionSet.{GetSubactionMemberName(subaction, conv)};");
                    }
                    else
                    {
                        appendLine($"actionStateGetInfo.subactionPath = XR_NULL_PATH;");
                    }

                    appendLine($"result = {GetActionStateUpdateFunction(action.Type)}(session, &actionStateGetInfo, &{GetActionStateMemberName(action, subaction, conv)});");

                    indent--;
                    appendLine("}");
                }
            }
            appendLine("return result;");
            indent--;
            appendLine("}");

            appendLine("");
            foreach (var action in actionSet.Actions)
            {
                if (action.Type == ActionType.Haptic)
                {
                    continue; // Skip out actions.
                }

                foreach (var subaction in GetActionSubactionPaths(actionSet, action, includeNull: true))
                {
                    appendLine($"{GetActionStateStructName(action.Type)} {GetActionStateMemberName(action, subaction, conv)}{{{GetActionStateStructType(action.Type)}}};");
                }
            }

            indent--;
            appendLine("};");
        }

        private static void WriteSuggestedBindingsStruct(StringBuilder sb, ActionSet actionSet, int indent, INamingConventionConverter conv)
        {
            Action<string> appendLineTrim = (string text) => sb.AppendLine(text.TrimEnd());
            Action<string> appendLine = (string text) => appendLineTrim($"{new string(' ', indent * 4)}{text}");
            Action<bool> addLineIfTrue = (bool condition) => { if (condition) { appendLineTrim(""); } };

            appendLine("");
            appendLine($"struct SuggestedBindings"); // TODO: Add a manifest name and use it here to ensure no namespace conflicts.
            appendLine("{");
            indent++;

            //  Initialize() method
            appendLine($"XrResult Initialize(XrInstance instance, {GetActionSetStructName(actionSet, conv)} const& actionSet)");
            appendLine("{");
            indent++;
            appendLine("XrResult result = XR_SUCCESS;");
            appendLine("");
            foreach (var suggestedBindings in actionSet.SuggestedBindings)
            {
                appendLine("if (XR_SUCCEEDED(result))");
                appendLine("{");
                indent++;
                appendLine($"result = xrStringToPath(instance, \"{suggestedBindings.InteractionProfile}\", &{GetInteractionProfileMemberName(suggestedBindings.InteractionProfile, conv)});");
                indent--;
                appendLine("}");
            }

            appendLine("");
            var allBindingPaths = actionSet.SuggestedBindings.SelectMany(s => s.Bindings).SelectMany(s => s.Value);
            foreach (var suggestedBinding in allBindingPaths.Distinct().OrderBy(p => p))
            {
                appendLine($"XrPath {GetBindingVariableName(suggestedBinding, conv)};");
            }

            foreach (var suggestedBinding in allBindingPaths.Distinct().OrderBy(p => p))
            {
                appendLine("if (XR_SUCCEEDED(result))");
                appendLine("{");
                indent++;
                appendLine($"result = xrStringToPath(instance, \"{suggestedBinding}\", &{GetBindingVariableName(suggestedBinding, conv)});");
                indent--;
                appendLine("}");
            }

            appendLine("");
            appendLine("if (XR_SUCCEEDED(result))");
            appendLine("{");
            indent++;
            foreach (var suggestedBindings in actionSet.SuggestedBindings)
            {
                int index = 0;
                foreach (var suggestedBinding in suggestedBindings.Bindings)
                {
                    foreach (var bindingPath in suggestedBinding.Value)
                    {
                        appendLine($"{GetSuggestedBindingsMemberName(suggestedBindings.InteractionProfile, conv)}[{index}] = {{actionSet.{GetActionHandleMemberName(suggestedBinding.Key, conv)}, {GetBindingVariableName(bindingPath, conv)}}};");
                        index++;
                    }
                }
            }
            indent--;
            appendLine("}");

            appendLine("");
            appendLine("return result;");
            indent--;
            appendLine("}");


            // SuggestInteractionProfileBindings() method
            appendLine("");
            appendLine($"XrResult SuggestInteractionProfileBindings(XrInstance instance)");
            appendLine("{");
            indent++;
            appendLine("XrResult result = XR_SUCCESS;");

            foreach (var suggestedBindings in actionSet.SuggestedBindings)
            {
                appendLine("");
                appendLine("if (XR_SUCCEEDED(result))");
                appendLine("{");
                indent++;
                appendLine($"XrInteractionProfileSuggestedBinding interactionSuggestedBindings{{XR_TYPE_INTERACTION_PROFILE_SUGGESTED_BINDING}};");
                appendLine($"interactionSuggestedBindings.interactionProfile = {GetInteractionProfileMemberName(suggestedBindings.InteractionProfile, conv)};");
                appendLine($"interactionSuggestedBindings.suggestedBindings = {GetSuggestedBindingsMemberName(suggestedBindings.InteractionProfile, conv)};");
                appendLine($"interactionSuggestedBindings.countSuggestedBindings = {suggestedBindings.Bindings.Sum(b => b.Value.Count())};");
                appendLine($"result = xrSuggestInteractionProfileBindings(instance, &interactionSuggestedBindings);");
                indent--;
                appendLine("}");
            }

            appendLine("");
            appendLine("return result;");
            indent--;
            appendLine("}");

            appendLine("");
            foreach (var suggestedBindings in actionSet.SuggestedBindings)
            {
                appendLine($"XrPath {GetInteractionProfileMemberName(suggestedBindings.InteractionProfile, conv)};");
            }

            appendLine("");
            foreach (var suggestedBindings in actionSet.SuggestedBindings)
            {
                appendLine($"XrActionSuggestedBinding {GetSuggestedBindingsMemberName(suggestedBindings.InteractionProfile, conv)}[{suggestedBindings.Bindings.Sum(b => b.Value.Count())}];");
            }

            indent--;
            appendLine("};");
        }
        public static string GetSuggestedBindingsMemberName(string interactionProfilePath, INamingConventionConverter conv) => conv.Rename(interactionProfilePath.Replace("/interaction_profiles/", "").Replace('/', '_'), "Bindings");

        public static string GetInteractionProfileMemberName(string interactionProfilePath, INamingConventionConverter conv) => conv.Rename(interactionProfilePath.Replace("/interaction_profiles/", "").Replace('/', '_'));

        public static string GetBindingVariableName(string bindingPath, INamingConventionConverter conv) => conv.Rename(bindingPath.Replace('/', '_'));

        private static string GetActionStateMemberName(Action action, TopLevelPath subactionPath, INamingConventionConverter conv) => conv.Rename(GetNameWithSubaction(action.Name, subactionPath), "ActionState");

        private static string GetActionSetStructName(ActionSet actionSet, INamingConventionConverter conv) => conv.Rename(actionSet.Name, "ActionSet");

        private static string GetActionHandleMemberName(string actionName, INamingConventionConverter conv) => conv.Rename(actionName);

        private static string GetActionStateStructName(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.Boolean => "XrActionStateBoolean",
                ActionType.Pose => "XrActionStatePose",
                ActionType.Haptic => "XrHapticVibration",
                ActionType.Float => "XrActionStateFloat",
                ActionType.Vector2f => "XrActionStateVector2f ",
                _ => throw new NotImplementedException($"Unexpected action type: {actionType}"),
            };
        }

        private static string GetActionStateStructType(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.Boolean => "XR_TYPE_ACTION_STATE_BOOLEAN",
                ActionType.Pose => "XR_TYPE_ACTION_STATE_POSE",
                ActionType.Haptic => "XR_TYPE_HAPTIC_VIBRATION",
                ActionType.Float => "XR_TYPE_ACTION_STATE_FLOAT",
                ActionType.Vector2f => "XR_TYPE_ACTION_STATE_VECTOR2F ",
                _ => throw new NotImplementedException($"Unexpected action type: {actionType}"),
            };
        }

        private static string GetNameWithSubaction(string name, TopLevelPath subaction)
        {
            string part = subaction switch
            {
                TopLevelPath.Null => "",
                TopLevelPath.UserHandLeft => "LeftHand",
                TopLevelPath.UserHandRight => "RightHand",
                _ => throw new ArgumentException($"Unsupported subaction '{subaction}'"),
            };
            return $"{name}_{part}";
        }

        private static string GetActionType(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.Boolean => "XR_ACTION_TYPE_BOOLEAN_INPUT",
                ActionType.Pose => "XR_ACTION_TYPE_POSE_INPUT",
                ActionType.Haptic => "XR_ACTION_TYPE_VIBRATION_OUTPUT",
                ActionType.Float => "XR_ACTION_TYPE_FLOAT_INPUT",
                ActionType.Vector2f => "XR_ACTION_TYPE_VECTOR2F_INPUT ",
                _ => throw new NotImplementedException($"Unexpected action type: {actionType}"),
            };
        }

        private static string GetActionStateUpdateFunction(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.Boolean => "xrGetActionStateBoolean",
                ActionType.Pose => "xrGetActionStatePose",
                ActionType.Float => "xrGetActionStateFloat",
                ActionType.Vector2f => "xrGetActionStateVector2f ",
                _ => throw new NotImplementedException($"Unexpected action type: {actionType}"),
            };
        }

        private static string GetSubactionMemberName(TopLevelPath subaction, INamingConventionConverter converter)
        {
            return subaction switch
            {
                TopLevelPath.UserHandLeft => converter.Rename("LeftHand"),
                TopLevelPath.UserHandRight => converter.Rename("RightHand"),
                _ => throw new ArgumentException($"Unsupported subaction '{subaction}'")
            };
        }

        private static string GetSubactionPath(TopLevelPath subactionPath)
        {
            return subactionPath switch
            {
                TopLevelPath.UserHandLeft => "/user/hand/left",
                TopLevelPath.UserHandRight => "/user/hand/right",
                _ => throw new ArgumentException($"Unsupported subactionPath in binding {subactionPath}"),
            };
        }

        private static IEnumerable<TopLevelPath> GetActionSubactionPaths(ActionSet actionSet, Action action, bool includeNull = false)
        {
            if (includeNull && !action.UseSubactionPaths)
            {
                return new[] { TopLevelPath.Null };
            }

            var allActionBindings = actionSet.SuggestedBindings.SelectMany(b => b.Bindings).Where(b => b.Key == action.Name).SelectMany(b => b.Value);

            var subactionPaths = new HashSet<TopLevelPath>();
            foreach (var binding in allActionBindings)
            {
                if (binding.StartsWith("/user/hand/left/"))
                {
                    subactionPaths.Add(TopLevelPath.UserHandLeft);
                }
                else if (binding.StartsWith("/user/hand/right/"))
                {
                    subactionPaths.Add(TopLevelPath.UserHandRight);
                }
                else
                {
                    throw new ArgumentException($"Unsupported subactionPath in binding {binding}");
                }
            }

            return subactionPaths;
        }
    }
}
