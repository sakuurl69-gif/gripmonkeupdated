using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.XR;

namespace ExtraGripMod
{
    [BepInPlugin("com.extragrip.gtag", "ExtraGrip", "1.0.0")]
    public class ExtraGrip : BaseUnityPlugin
    {
        ConfigEntry<bool> enabled;
        ConfigEntry<float> slideControl;
        ConfigEntry<float> gripStrength;
        ConfigEntry<bool> requireGripButton;

        object player;
        Type playerType;
        Rigidbody rb;
        FieldInfo leftTouch, rightTouch, slideField, defaultSlideField;
        bool searched;
        float nextSearch;

        void Awake()
        {
            enabled = Config.Bind("General", "Enabled", true, "Turn the mod on/off");
            slideControl = Config.Bind("Grip", "SlideControl", 1f, "1 = max grip (no sliding on surfaces), lower = more slippery");
            gripStrength = Config.Bind("Grip", "GripStrength", 0.35f, "How hard your motion is damped while holding on (0-1)");
            requireGripButton = Config.Bind("Grip", "RequireGripButton", true, "Only hold on while a grip button is pressed");
            Logger.LogInfo("ExtraGrip loaded");
        }

        void FindPlayer()
        {
            nextSearch = Time.time + 2f;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = a.GetType("GorillaLocomotion.GTPlayer");
                if (t == null) t = a.GetType("GorillaLocomotion.Player");
                if (t == null) continue;
                BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
                object inst = null;
                PropertyInfo p = t.GetProperty("Instance", all);
                if (p != null) inst = p.GetValue(null, null);
                if (inst == null)
                {
                    FieldInfo f = t.GetField("Instance", all) ?? t.GetField("_instance", all);
                    if (f != null) inst = f.GetValue(null);
                }
                if (inst == null) continue;

                player = inst;
                playerType = t;
                BindingFlags inst2 = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                leftTouch = t.GetField("wasLeftHandTouching", inst2);
                rightTouch = t.GetField("wasRightHandTouching", inst2);
                slideField = t.GetField("slideControl", inst2);
                defaultSlideField = t.GetField("defaultSlideControl", inst2);
                Component c = inst as Component;
                if (c != null) rb = c.GetComponent<Rigidbody>();
                Logger.LogInfo("Found player type: " + t.FullName);
                return;
            }
        }

        bool GripHeld(XRNode node)
        {
            InputDevice d = InputDevices.GetDeviceAtXRNode(node);
            bool v;
            return d.isValid && d.TryGetFeatureValue(CommonUsages.gripButton, out v) && v;
        }

        bool Touching(FieldInfo f)
        {
            if (f == null) return false;
            try { return (bool)f.GetValue(player); } catch { return false; }
        }

        void Update()
        {
            if (!enabled.Value) return;
            if (player == null || (player as UnityEngine.Object) == null)
            {
                if (Time.time >= nextSearch) FindPlayer();
                return;
            }

            // More grip: stop the slide on surfaces
            try
            {
                if (slideField != null) slideField.SetValue(player, slideControl.Value);
                if (defaultSlideField != null) defaultSlideField.SetValue(player, slideControl.Value);
            }
            catch { }

            // Extra hold while a gripping hand is touching something
            if (rb == null) return;
            bool left = Touching(leftTouch) && (!requireGripButton.Value || GripHeld(XRNode.LeftHand));
            bool right = Touching(rightTouch) && (!requireGripButton.Value || GripHeld(XRNode.RightHand));
            if (left || right)
            {
                float k = Mathf.Clamp01(gripStrength.Value);
                rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, k);
            }
        }
    }
}
