using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace FraggleExpansion
{
    public class PropertiesReader 
    {
        public PropertiesReader() => InitializeData();

        public void InitializeData()
        {
         
            List<string> CleanContent = new List<string>();
            string FilePath =
            Path.Combine(BepInEx.Paths.GameRootPath + "\\BepInEx\\plugins\\CreativeExpansionPack\\ExpansionData.txt");
            if (!File.Exists(FilePath)) { Application.Quit(); return; }
            string[] AllLinesInFile = File.ReadAllLines(FilePath);

            string[] CommentOptions =
            {
                "//",
                "///",
                "#"
            };

            // Get only relevant data
            foreach (string Line in AllLinesInFile)
            foreach (string Comment in CommentOptions)
            if (Line.StartsWith(Comment) || Line == "") continue;
            else CleanContent.Add(Line);

            // Configure data
            foreach (string Data in CleanContent)
            {
                string[] SplitData = Data.Split(':');
                bool ResultAsBoolean = true;
             // float ResultAsFloat = 0f;

                switch (SplitData[0])
                {
                    // Booleans

                    case "removecostandstock":
                        ReadBool(SplitData[1], ref ResultAsBoolean, FraggleExpansionData.RemoveCostAndStock);
                        FraggleExpansionData.RemoveCostAndStock = ResultAsBoolean;
                        break;

                    case "removerotation":
                        ReadBool(SplitData[1], ref ResultAsBoolean, FraggleExpansionData.RemoveRotation);
                        FraggleExpansionData.RemoveRotation = ResultAsBoolean;
                        break;

                    case "bypassbounds":
                        ReadBool(SplitData[1], ref ResultAsBoolean, FraggleExpansionData.BypassBounds);
                        FraggleExpansionData.BypassBounds = ResultAsBoolean;
                        break;

                    case "betawalls":
                        ReadBool(SplitData[1], ref ResultAsBoolean, FraggleExpansionData.BetaWalls);
                        FraggleExpansionData.BetaWalls = ResultAsBoolean;
                        break;

                    case "displaylevel":
                        ReadBool(SplitData[1], ref ResultAsBoolean, FraggleExpansionData.DisplayLevel);
                        FraggleExpansionData.DisplayLevel = ResultAsBoolean;
                        break;

                    case "exploreskin":
                        ReadBool(SplitData[1], ref ResultAsBoolean, FraggleExpansionData.UseMainSkinInExploreState);
                        FraggleExpansionData.UseMainSkinInExploreState = ResultAsBoolean;
                        break;

                    case "lastposition":
                        ReadBool(SplitData[1], ref ResultAsBoolean, FraggleExpansionData.LastPostion);
                        FraggleExpansionData.LastPostion = ResultAsBoolean;
                        break;

                    case "letobjectsclip":
                        ReadBool(SplitData[1], ref ResultAsBoolean, FraggleExpansionData.CanClipObjects);
                        FraggleExpansionData.CanClipObjects = ResultAsBoolean;
                        break;

                    case "customtestmusic":
                        ReadBool(SplitData[1], ref ResultAsBoolean, FraggleExpansionData.CustomTestMusic);
                        FraggleExpansionData.CustomTestMusic = ResultAsBoolean;
                        break;

                    case "insanepaintersize":
                        ReadBool(SplitData[1], ref ResultAsBoolean, FraggleExpansionData.InsanePainterSize);
                        FraggleExpansionData.InsanePainterSize = ResultAsBoolean;
                        break;

                    case "addunusedobjects":
                        ReadBool(SplitData[1], ref ResultAsBoolean, FraggleExpansionData.AddUnusedObjects);
                        FraggleExpansionData.AddUnusedObjects = ResultAsBoolean;
                        break;

                    case "musiceventplaymode":
                        FraggleExpansionData.MusicEventPlayMode = SplitData[1];
                        break;

                    case "musicbankplaymode":
                        FraggleExpansionData.MusicBankPlayMode = SplitData[1];
                        break;



                    // CEP generated and pretty bad, it works tho...
                    // Do not touch (I know it's bad but still)

                    case "letfirsttimepopuphappen":
                        ReadBool(SplitData[1], ref ResultAsBoolean, FraggleExpansionData.LetFirstTimePopUpHappen);
                        FraggleExpansionData.LetFirstTimePopUpHappen = ResultAsBoolean;
                        break;
                }

            }
        }

        public static void WriteFirstTimePopUpGone(string Prop)
        {
            if (!FraggleExpansionData.LetFirstTimePopUpHappen) return;
            string FilePath = Path.Combine(BepInEx.Paths.GameRootPath + "\\BepInEx\\plugins\\CreativeExpansionPack\\ExpansionData.txt");
            
            // Totally didn't ChatGPT out of laziness
            using (StreamWriter Writer = File.AppendText(FilePath))
            {
                Writer.WriteLine(Prop + ":false");
            }
        }

        public void ReadBool(string Data, ref bool Value, bool BaseResult)
        {
            Value = BaseResult;
            if (Data.ToLower() == "true") Value = true;
            else if (Data.ToLower() == "false") Value = false;
        }

        /*
        public void ReadFloat(string Data, ref float Value, float BaseResult)
        {
            Value = BaseResult;
            try { Value = float.Parse(Data); } catch { }
        }
        */
    }

    public struct FraggleExpansionData
    {

        // True booleans
        public static bool AddUnusedObjects, InsanePainterSize ,CustomTestMusic, RemoveCostAndStock, CanClipObjects ,LastPostion, RemoveRotation, BypassBounds, BetaWalls, DisplayLevel, UseMainSkinInExploreState = true;
        public static bool LetFirstTimePopUpHappen = true;
        public static string MusicBankPlayMode = "BNK_Music_Long_Wall";
        public static string MusicEventPlayMode = "MUS_InGame_Long_Wall";

    }
}
