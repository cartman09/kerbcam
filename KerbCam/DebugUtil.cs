﻿using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace KerbCam {
    class DebugUtil {
        public static void LogException(Exception e) {
            try {
                Debug.LogError(e.ToString() + "\n" + e.StackTrace);
            } catch (Exception) {
                Debug.LogError("KerbCam failed to log an exception");
            }
        }

        public static void LogCameras() {
            // Display full camera list and basic information about each.
            foreach (var cam in Camera.allCameras) {
                LogCamera(cam);
            }
        }

        public static void LogVessel(Vessel v) {
            Log("Vessel name={0}", v.name);
        }

        public static void LogCamera(Camera cam) {
            Log("Camera ID {0} name={1} is_current={2} is_main={3} " +
                "enabled={4} active_self={5} active_hierarchy={6} " +
                "depth={7} tag={8}",
                cam.GetHashCode(),
                cam.name,
                cam == Camera.current,
                cam == Camera.main,
                cam.enabled,
                cam.gameObject.activeSelf,
                cam.gameObject.activeInHierarchy,
                cam.depth,
                cam.tag);
        }

        public static void LogCamerasTransformTree() {
            // Root transform IDs to the root transform of that ID.
            var trnRoots = new Dictionary<int, Transform>();
            // Transform IDs to cameras using that transform.
            var trnCams = new Dictionary<int, List<Camera>>();
            foreach (Camera c in Camera.allCameras) {
                Transform root = c.transform.root;
                trnRoots[root.GetInstanceID()] = root;

                int camTrnId = c.transform.GetInstanceID();
                List<Camera> camList;
                if (!trnCams.TryGetValue(camTrnId, out camList)) {
                    trnCams[camTrnId] = camList = new List<Camera>();
                }
                camList.Add(c);
            }

            var result = new StringBuilder();
            foreach (Transform root in trnRoots.Values) {
                result.AppendLine("--------");
                if (root.name == "_UI") continue;
                AppendCameraTransform(result, 0, root, trnCams);
            }
            Debug.Log(result.ToString());
        }

        private static void AppendCameraTransform(
            StringBuilder result, int level, Transform trn,
            Dictionary<int, List<Camera>> trnCams) {

            Component[] cmps = trn.gameObject.GetComponents<Component>();
            string[] cmpStrs = new string[cmps.Length];
            for (int i = 0; i < cmps.Length; i++) {
                cmpStrs[i] = cmps[i].GetType().Name;
            }

            result.AppendFormat(
                "{0} {1} [{2}]",
                new string('+', level), trn.name, string.Join(", ", cmpStrs));
            result.AppendLine();

            int numChildTrns = trn.GetChildCount();
            foreach (Transform child in trn) {
                AppendCameraTransform(result, level + 1, child, trnCams);
            }
        }

        public static string Format(Transform trn) {
            return String.Format(
                "locPos={0} locRot={1} pos={2} rot={3}",
                trn.localPosition, trn.localRotation,
                trn.position, trn.rotation);
        }

        public static void LogTransformAscestry(Transform trn) {
            int i = 0;
            for (; trn != null; trn = trn.parent, i++) {
                Log("#{0} {1}", i, Format(trn));
            }
        }

        public static void Log(string fmt, params object[] args) {
            Debug.Log(string.Format(fmt, args));
        }
    }
}
