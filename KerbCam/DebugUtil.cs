﻿using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace KerbCam {
    class DebugUtil {
        public static void LogException(Exception e) {
            try {
                Debug.LogError(e.ToString());
            } catch (Exception) {
                Debug.LogError("KerbCam failed to log an exception");
            }
        }

        public static string NameOfMaybeNull(Component c) {
            return c == null ? "<null>" : c.name;
        }

        public static void LogVessel(Vessel v) {
            Log("Vessel name={0}", v.name);
        }

        public static void LogCamerasTransformTree() {
            // Root transform IDs to the root transform of that ID.
            var trnRoots = new Dictionary<int, Transform>();
            // Transform IDs to cameras using that transform.
            var trnCams = new Dictionary<int, List<UnityEngine.Camera>>();
            // Transform IDs that are in the ancestry of cameras.
            var trnCamAncestors = new Dictionary<int, bool>();
            foreach (UnityEngine.Camera c in UnityEngine.Camera.allCameras) {
                Transform root = c.transform.root;
                trnRoots[root.GetInstanceID()] = root;

                int camTrnId = c.transform.GetInstanceID();
                List<UnityEngine.Camera> camList;
                if (!trnCams.TryGetValue(camTrnId, out camList)) {
                    trnCams[camTrnId] = camList = new List<UnityEngine.Camera>();
                }
                camList.Add(c);

                for (var trn = c.transform; trn != null; trn = trn.parent) {
                    trnCamAncestors[trn.GetInstanceID()] = true;
                }
            }

            var result = new StringBuilder();
            foreach (Transform root in trnRoots.Values) {
                result.AppendLine("--------");
                /*string skipReason = null;
                if (object.ReferenceEquals(root.gameObject, ScreenSafeUI.fetch)) {
                    skipReason = "ScreenSafeUI";
                } else if (root.name == "_UI") {
                    skipReason = "_UI";
                }
                if (skipReason != null) {
                    result.AppendFormat("(skipping {0} transform tree)\n", skipReason);
                    continue;
                }*/
                AppendCameraTransform(result, 0, root, trnCams, trnCamAncestors);
            }
            Debug.Log(result.ToString());
        }

        private static void AppendCameraTransform(
            StringBuilder result, int level, Transform trn,
            Dictionary<int, List<UnityEngine.Camera>> trnCams, Dictionary<int, bool> trnCamAncestors) {

            bool isAncestor = trnCamAncestors.ContainsKey(trn.GetInstanceID());

            Component[] cmps = trn.gameObject.GetComponents<Component>();
            string[] cmpStrs = new string[cmps.Length];
            for (int i = 0; i < cmps.Length; i++) {
                cmpStrs[i] = cmps[i].GetType().Name;
            }

            result.Append('+', level);
            result.AppendFormat("{0} [{1}]", trn.name, string.Join(", ", cmpStrs));
            if (isAncestor) {
                result.Append(' ');
                result.Append(Format(trn));
                result.AppendLine();
                // Only descend into descendents of transforms that are ancestors of
                // cameras. This logs helpful context for camera transforms, without
                // showing excessive internal model information.
                int numChildTrns = trn.childCount;
                foreach (Transform child in trn) {
                    AppendCameraTransform(result, level + 1, child, trnCams, trnCamAncestors);
                }
                return;
            }
            
            if (trn.childCount > 0) {
                result.Append(" (descendents hidden)");
            }
            result.AppendLine();
        }

        public static string Format(Transform trn) {
            return String.Format(
                "name={0} locPos={1} locRot={2}",
                trn.name, trn.localPosition, trn.localRotation);
        }

        public static void LogTransformAscestry(Transform trn) {
            var result = new StringBuilder();
            int i = 0;
            for (; trn != null; trn = trn.parent, i++) {
                result.AppendFormat("#{0} {1}\n", i, Format(trn));
            }
            Log(result.ToString());
        }

        public static void Log(string fmt, params object[] args) {
            Debug.Log(string.Format(fmt, args));
        }
    }
}
