﻿/*
 *   This file is part of Orbital Material Science.
 *   
 *   Part of the code may originate from Station Science ba ether net http://forum.kerbalspaceprogram.com/threads/54774-0-23-5-Station-Science-(fourth-alpha-low-tech-docking-port-experiment-pod-models)
 * 
 *   Orbital Material Science is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   Orbital Material Sciencee is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with Orbital Material Science.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NE_Science
{
    [KSPAddon(KSPAddon.Startup.EveryScene, true)]
    class NE_Helper : MonoBehaviour
    {

        private static string SETTINGS_FILE;
        private const string DEBUG_VALUE = "Debug";
        private static bool debug = true;


        void Start()
        {
            if (String.IsNullOrEmpty(SETTINGS_FILE)) {
                SETTINGS_FILE = KSPUtil.ApplicationRootPath + "GameData/NehemiahInc/NE_Science_Common/Resources/settings.cfg";
            }
            ConfigNode settings = getSettingsNode();
            bool d = false;
            try
            {
                d = bool.Parse(settings.GetValue(DEBUG_VALUE));
            }
            catch (FormatException e)
            {
                d = true;
                NE_Helper.logError("Loading Settings: " + e.Message);
            }
            NE_Helper.debug = d;
            DontDestroyOnLoad(this);
        }

        private ConfigNode getSettingsNode()
        {
            ConfigNode node = ConfigNode.Load(SETTINGS_FILE);
            if (node == null)
            {
                return createSettings();
            }
            return node;
        }

        private ConfigNode createSettings(){

            ConfigNode node = new ConfigNode();
            node.AddValue(DEBUG_VALUE, false);
            node.Save(SETTINGS_FILE);
            return node;
        }

        /// <summary>
        /// Returns the ConfigNode's value as an int, or 0 on failure.
        /// </summary>
        /// <returns>The node value as int.</returns>
        /// <param name="node">The Node from which to retrieve the Value.</param>
        /// <param name="name">The name of the Value to retrieve.</param>
        public static int GetValueAsInt(ConfigNode node, string name)
        {
            int rv = 0;
            try {
                if (!node.TryGetValue(name, ref rv))
                {
                    rv = 0;
                }
            } catch (Exception e) {
                logError("GetValueAsInt - exception: " + e.Message);
            }
            return rv;
        }

        /// <summary>
        /// Returns the ConfigNode's value as a float, or 0f on failure.
        /// </summary>
        /// <returns>The node value as float.</returns>
        /// <param name="node">The Node from which to retrieve the Value.</param>
        /// <param name="name">The name of the Value to retrieve.</param>
        public static float GetValueAsFloat(ConfigNode node, string name)
        {
            float rv = 0f;
            try {
                if (!node.TryGetValue(name, ref rv))
                {
                    rv = 0f;
                }
            } catch (Exception e) {
                logError("GetValueAsFloat - exception: " + e.Message);
            }
            return rv;
        }

        public static bool debugging()
        {
            return debug;
        }

        public static void log( string msg)
        {
            if (debug)
            {
                Debug.Log("[NE] " + msg);
            }
        }

        public static void logError(string errMsg){
            Debug.LogError("[NE] Error: " + errMsg);
        }
    }
}
