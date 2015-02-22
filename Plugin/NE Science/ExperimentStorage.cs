﻿/*
 *   This file is part of Orbital Material Science.
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
    public interface ExperimentDataStorage
    {
        void removeExperimentData();

        GameObject getPartGo();

        Part getPart();
    }

    public class ExperimentStorage : ModuleScienceExperiment, ExperimentDataStorage
    {

        [KSPField(isPersistant = false)]
        public string identifier = "";

        [KSPField(isPersistant = false)]
        public bool chanceTexture = false;

        [KSPField(isPersistant = true)]
        public string type = ExperimentFactory.OMS_EXPERIMENTS;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Contains")]
        public string contains = "";

        private ExperimentData expData = ExperimentData.getNullObject();
        private int count = 0;

        private List<ExperimentData> availableExperiments
            = new List<ExperimentData>();
        List<Lab> availableLabs = new List<Lab>();

        private int showGui = 0;
        private Rect finalizeWindowRect = new Rect(Screen.width / 2 - 160, Screen.height / 4, 320, 120);
        private Rect addWindowRect = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 250, 200, 400);
        private Vector2 addScrollPos = new Vector2();
        private Rect labWindowRect = new Rect(Screen.width - 250, Screen.height / 2 - 250, 200, 400);
        private Vector2 labScrollPos = new Vector2();

        private ExpContainerTextureFactory textureReg = new ExpContainerTextureFactory();
        private Material contMat;
        private int windowID;
        

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            NE_Helper.log("ExperimentStorage: OnLoad");

            ConfigNode expNode = node.GetNode(ExperimentData.CONFIG_NODE_NAME);
            if (expNode != null)
            {
                setExperiment(ExperimentData.getExperimentDataFromNode(expNode));
            }
            else
            {
                setExperiment(ExperimentData.getNullObject());
            }
        }

        private void setExperiment(ExperimentData experimentData)
        {
            NE_Helper.log("MOVExp.setExp() id: " + experimentData.getId());
            expData = experimentData;
            contains = expData.getAbbreviation();
            expData.setStorage(this);

            experimentID = expData.getId();
            experiment = ResearchAndDevelopment.GetExperiment(experimentID);

            experimentActionName = "Results";
            resetActionName = "Throw Away Results";
            reviewActionName = "Review " + expData.getAbbreviation() + " Results";

            useStaging = false;
            useActionGroups = true;
            hideUIwhenUnavailable = true;
            resettable = false;
            resettableOnEVA = false;

            dataIsCollectable = false;
            collectActionName = "Collect Results";
            interactionRange = 1.2f;
            xmitDataScalar = 0.05f;
            if (chanceTexture)
            {
                setTexture(expData);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddNode(expData.getNode());
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (state.Equals(StartState.Editor))
            {
                Events["chooseEquipment"].active = true;
            }
            else
            {
                Events["chooseEquipment"].active = false;
            }
            Events["DeployExperiment"].active = false;
        }

        public override void OnUpdate()
        {

            base.OnUpdate();
            if (count == 0)
            {
                Events["installExperiment"].active = expData.canInstall(part.vessel);
                if (Events["installExperiment"].active)
                {
                    Events["installExperiment"].guiName = "Install " + expData.getAbbreviation();
                }
                Events["moveExp"].active = expData.canMove(part.vessel);
                if (Events["moveExp"].active)
                {
                    Events["moveExp"].guiName = "Move " + expData.getAbbreviation();
                }
                Events["finalize"].active = expData.canFinalize();
                if (Events["installExperiment"].active)
                {
                    Events["finalize"].guiName = "Finalize " + expData.getAbbreviation();
                }
                Events["DeployExperiment"].active = false;
            }
            count = (count + 1) % 3;

        }

        public new void DeployExperiment()
        {
            NE_Helper.log("DeployExperiment called");
            if (expData.canFinalize())
            {
                base.DeployExperiment();
                expData.finalize();
            }
            else
            {
                ScreenMessages.PostScreenMessage("Experiment " + expData.getAbbreviation() + " is not finished. Run the experiment first!!!" , 6, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        public new void ResetExperiment()
        {
            NE_Helper.log("ResetExperiment");
            base.ResetExperiment();
        }

        public new void ResetExperimentExternal()
        {
            NE_Helper.log("ResetExperimentExpernal");
            base.ResetExperimentExternal();
        }

        public new void ResetAction(KSPActionParam p)
        {
            NE_Helper.log("ResetAction");
            base.ResetAction(p);
        }

        public new void DumpData(ScienceData data)
        {
            NE_Helper.log("DumbData");
            base.DumpData(data);
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Add Experiment", active = false)]
        public void chooseEquipment()
        {
            if (expData.getId() == "")
            {
                availableExperiments = ExperimentFactory.getAvailableExperiments(type);
                windowID = WindowCounter.getNextWindowID();
                showGui = 1;
            }
            else
            {
                removeExperimentData();
                Events["chooseEquipment"].guiName = "Add Experiment";
            }
        }

        [KSPEvent(guiActive = true, guiName = "Install Experiment", active = false)]
        public void installExperiment()
        {
            availableLabs = expData.getFreeLabsWithEquipment(part.vessel);
            if (availableLabs.Count > 0)
            {
                if (availableLabs.Count == 1)
                {
                    installExperimentInLab(availableLabs[0]);
                }
                else
                {
                    windowID = WindowCounter.getNextWindowID();
                    showGui = 3;
                }
            }
            else
            {
                NE_Helper.logError("Experiment install: No lab found");
            }
        }

        private void installExperimentInLab(Lab lab)
        {
            lab.installExperiment(expData);
            removeExperimentData();
        }

        [KSPEvent(guiActive = true, guiName = "Move Experiment", active = false)]
        public void moveExp()
        {
            expData.move(part.vessel);
        }

        [KSPEvent(guiActive = true, guiName = "Finalize Experiment", active = false)]
        public void finalize()
        {
            windowID = WindowCounter.getNextWindowID();
            showGui = 2;
        }

        void OnGUI()
        {
            switch (showGui)
            {
                case 1:
                    showAddWindow();
                    break;
                case 2:
                    showFinalizeWaring();
                    break;
                case 3:
                    showLabWindow();
                    break;

            }
        }

        private void showLabWindow()
        {
            labWindowRect = GUI.Window(windowID, labWindowRect, showLabGui, "Install Experiment");
        }

        void showLabGui(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Choose Lab");
            labScrollPos = GUILayout.BeginScrollView(labScrollPos, GUILayout.Width(180), GUILayout.Height(320));
            int i = 0;
            foreach (Lab l in availableLabs)
            {
                if (GUILayout.Button(new GUIContent(l.abbreviation, i.ToString())))
                {
                    installExperimentInLab(l);
                    closeGui();
                }
                ++i;
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close"))
            {
                closeGui();
            }
            GUILayout.EndVertical();

            String hover = GUI.tooltip;
            try
            {
                int hoverIndex = int.Parse(hover);
                availableLabs[hoverIndex].part.SetHighlightColor(Color.cyan);
                availableLabs[hoverIndex].part.SetHighlightType(Part.HighlightType.AlwaysOn);
                availableLabs[hoverIndex].part.SetHighlight(true, false);
            }
            catch (FormatException)
            {
                resetHighlight();
            }
            GUI.DragWindow();
        }

        private void closeGui()
        {
            resetHighlight();
            showGui = 0;
        }

        private void resetHighlight()
        {
            foreach (Lab l in availableLabs)
            {
                l.part.SetHighlightDefault();
            }
        }

        private void showFinalizeWaring()
        {
            finalizeWindowRect = GUI.ModalWindow(7909032, finalizeWindowRect, finalizeWindow, "Finalize " + expData.getAbbreviation() + " Experiment");
        }

        void finalizeWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("You can no longer move the experiment after finalization.");
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
            {
                showGui = 0;
            }
            if (GUILayout.Button("OK"))
            {
                DeployExperiment();
                showGui = 0;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        

        private void showAddWindow()
        {
            addWindowRect = GUI.Window(windowID, addWindowRect, showAddGUI, "Add Experiment");
        }
        private void showAddGUI(int id)
        {

            GUILayout.BeginVertical();
            addScrollPos = GUILayout.BeginScrollView(addScrollPos, GUILayout.Width(180), GUILayout.Height(350));
            foreach (ExperimentData e in availableExperiments)
            {
                if (GUILayout.Button(e.getAbbreviation()))
                {
                    setExperiment(e);
                    NE_Helper.log(e.getNode().ToString());
                    part.mass += e.getMass();
                    Events["chooseEquipment"].guiName = "Remove " + e.getAbbreviation();
                    showGui = 0;
                }
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close"))
            {
                showGui = 0;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public bool isEmpty()
        {
            return expData == null || expData.getId() == "";
        }

        internal void storeExperiment(ExperimentData experimentData)
        {
            setExperiment(experimentData);
        }

        public void removeExperimentData()
        {
            part.mass -= expData.getMass();
            setExperiment(ExperimentData.getNullObject());
        }

        public GameObject getPartGo()
        {
            return part.gameObject;
        }

        public Part getPart()
        {
            return part;
        }

        private void setTexture(ExperimentData expData)
        {
            GameDatabase.TextureInfo tex = textureReg.getTextureForExperiment(expData);
            if (tex != null)
            {
                changeTexture(tex);
            }
            else
            {
                NE_Helper.logError("Change Experiment Container Texure: Texture Null");
            }
        }

        private void changeTexture(GameDatabase.TextureInfo newTexture)
        {
            Material mat = getContainerMaterial();
            if (mat != null)
            {
                mat.mainTexture = newTexture.texture;
            }
            else
            {
                NE_Helper.logError("Transform NOT found: " + "Equipment Container");
            }
        }

        private Material getContainerMaterial()
        {
            if (contMat == null)
            {
                Transform t = part.FindModelTransform("Experiment");
                if (t != null)
                {
                    contMat = t.renderer.material;
                    return contMat;
                }
                else
                {
                    NE_Helper.logError("Experiment Container Material null");
                    return null;
                }
            }
            else
            {
                return contMat;
            }
        }
    }

    class ExpContainerTextureFactory
    {
        private Dictionary<string, GameDatabase.TextureInfo> textureReg = new Dictionary<string, GameDatabase.TextureInfo>();
        private string folder = "NehemiahInc/Parts/ExperimentContainer/";
        private Dictionary<string, string> textureNameReg = new Dictionary<string, string>() { { "", "ExperimentContainerTexture" },
        { "FLEX", "FlexContainerTexture" }, { "CFI", "CfiContainerTexture" }, { "CCF", "CcfContainerTexture" },
        { "CFE", "CfeContainerTexture" }, { "MIS1", "Msi1ContainerTexture" }, { "MIS2", "Msi2ContainerTexture" }, { "MIS3", "Msi3ContainerTexture" },
        { "MEE1", "Mee1ContainerTexture" }, { "MEE2", "Mee2ContainerTexture" }, { "CVB", "CvbContainerTexture" }, { "PACE", "PACEContainerTexture" },
        { "ADUM", "AdumContainerTexture" }, { "SpiU", "SpiuContainerTexture" }};


        internal GameDatabase.TextureInfo getTextureForExperiment(ExperimentData expData)
        {
            GameDatabase.TextureInfo tex;
            if (textureReg.TryGetValue(expData.getType(), out tex))
            {
                return tex;
            }
            else
            {
                NE_Helper.log("Loading Texture for experiment: " + expData.getType());
                GameDatabase.TextureInfo newTex = getTexture(expData.getType());
                if(newTex != null){
                    textureReg.Add(expData.getType(), newTex);
                    return newTex;
                }
            }

            return null;
        }

        private GameDatabase.TextureInfo getTexture(string p)
        {
            string textureName;
            if(textureNameReg.TryGetValue(p, out textureName)){
                GameDatabase.TextureInfo newTex = GameDatabase.Instance.GetTextureInfoIn(folder, textureName);
                if (newTex != null)
                {
                    return newTex;
                }
            }
            NE_Helper.logError("Could not load texture for Exp: " + p);
            return null;
        }
    }
}
