using Il2CppSystem;
using MelonLoader;
using RUMBLE.Environment.Howard;
using RUMBLE.Interactions.InteractionBase;
using RUMBLE.MoveSystem;
using System.IO;
using TMPro;
using UnityEngine;

namespace CustomHoward
{
    public class LogicData : MonoBehaviour
    {
        public string LogicName { get; set; }
        public float MaxHP { get; set; }
        public float DecisionMin { get; set; }
        public float DecisionMax { get; set; }
        public float ReactionTime { get; set; }
        public Color HeadLight { get; set; }
        public Color IdleColor { get; set; }
        public Color ActiveColor { get; set; }
        public float Chance_DoSeq { get; set; }
        public float Chance_Dodge { get; set; }
        public float Chance_Nothing { get; set; }

    }
    public class SeqGenData : MonoBehaviour
    {
        public string SeqName { get; set; }
        public string AtkName { get; set; }
        public float Weight { get; set; }
        public float WeightDec { get; set; }
        public float PreWait { get; set; }
        public float PostWait { get; set;}
        public float RangeMin { get; set; }
        public float RangeMax { get; set; }

    }
    public class PoseGenData : MonoBehaviour
    {
        public int StackNumber { get; set; }
        public float PreWait { get; set; }
        public float PostWait { get; set; }
        public string AnimationName { get; set; }

    }
    public class CustomHowardClass : MelonMod
    {
        //constants
        private const double SceneDelay = 8.0;
        private const string BaseFolder = "UserData";
        private const string ModFolder = "CustomHoward";
        private const string MoveFolder = "CustomMoveSet";

        //constants - Stack Numbers
        private const int Stack_Explode = 0;
        //private const int Stack_Flick = 1;
        //private const int Stack_HoldR = 2;
        private const int Stack_Parry = 3;
        //private const int Stack_HoldL = 4;
        private const int Stack_Cube = 5;
        //private const int Stack_Dash = 6;
        private const int Stack_Uppercut = 7;
        private const int Stack_Wall = 8;
        //private const int Stack_Jump = 9;
        private const int Stack_Kick = 10;
        private const int Stack_Ball = 11;
        private const int Stack_Stomp = 12;
        private const int Stack_Pillar = 13;
        private const int Stack_Straight = 14;
        //private const int Stack_Disc = 15;

        //constants - animations
        private const string Anim_Straight = "Straight";
        private const string Anim_Kick = "Kick";
        private const string Anim_Spawn = "SpawnStructure";

        //variables
        private readonly bool debug = false;
        private readonly bool debug2 = false;
        private readonly bool debug3 = false;
        private bool loaddelaydone = false;
        private bool loadlockout = false;

        private int currentlogicindex = 0;

        private string currentscene;
        private string MoveSetPath;
        private string CurrentLogicName;

        private DateTime loaddelay;
        private DateTime ButtonDelay = DateTime.Now;

        //objects/collections
        private GameObject Howard_Obj;
        private HowardLogic[] Base_Logic_Array = new HowardLogic[3];
        private Il2CppSystem.Collections.Generic.List<Stack> StackList;
        private GameObject LogicText_Obj;
        private GameObject Lamp_Obj;

        //initializes things
        public override void OnLateInitializeMelon()
        {
            base.OnLateInitializeMelon();
            CheckandCreateFolder(BaseFolder + @"\" + ModFolder);
            CheckandCreateFolder(BaseFolder + @"\" + ModFolder + @"\" + MoveFolder);
            MoveSetPath = BaseFolder + @"\" + ModFolder + @"\" + MoveFolder + @"\";
        }

        //Run every update
        public override void OnUpdate()
        {
            //Base Updates
            base.OnUpdate();

            LoadDelayLogic();

            //There is no Howard outside the Gym
            if (currentscene == "Gym" && loaddelaydone)
            {
                if (Howard_Obj == null)
                {
                    Howard_Obj = GameObject.Find("--------------LOGIC--------------/Heinhouser products/Howard root");
                    if (debug) MelonLogger.Msg("Got Howard Object");

                    Howard_Obj.GetComponent<Howard>().howardAnimator.changeLevelAnimationWaitTime = 0.5f;
                    if (debug) MelonLogger.Msg("Changed Animation Speed");

                    Base_Logic_Array = Howard_Obj.GetComponent<Howard>().LogicLevels;
                    if (debug) MelonLogger.Msg("Got Base Logic Objects");

                    StackList = GameObject.Find("Player Controller(Clone)").GetComponent<PlayerStackProcessor>().availableStacks;
                    if (debug) MelonLogger.Msg("Available Stacks:");

                    foreach (var Stack in StackList)
                    {
                        if (debug) MelonLogger.Msg(Stack.name);
                    }

                    GetFromFile();
                    if (debug) MelonLogger.Msg("GotFromFile");

                    ModifyHowardConsole();

                    LogicText_Obj = GameObject.Find("--------------LOGIC--------------/Heinhouser products/Howard root/Howards console/Base Plate Custom/Upper Canvas/LogicText");
                    Lamp_Obj = GameObject.Find("--------------LOGIC--------------/Heinhouser products/Howard root/Howards console/Base Plate Custom/Status Lamp");
                }
                else
                {
                    //Buttons
                    if (Input.GetKeyDown(KeyCode.KeypadPlus))
                    {
                        ButtonHandler(1);
                    }
                    if (Input.GetKeyDown(KeyCode.KeypadMinus))
                    {
                        ButtonHandler(0);
                    }
                    if (Input.GetKeyDown(KeyCode.Keypad0))
                    {
                        ButtonHandler(2);
                    }

                    if (ButtonDelay <= DateTime.Now)
                    {
                        Lamp_Obj.transform.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.green);
                    }
                    else
                    {
                        Lamp_Obj.transform.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.red);
                    }
                }

            }

        }


        //Custom Movesets
        public HowardLogic.SequenceSet CreatePoseSequence(System.Collections.Generic.List<PoseGenData> PGD,SeqGenData SGD)
        {
            HowardLogic.SequenceSet Set = new HowardLogic.SequenceSet();
            HowardSequence Sequence = new HowardSequence();
            HowardSequence.HowardBehaviourTiming[] Timings = new HowardSequence.HowardBehaviourTiming[1];
            HowardAttackBehaviour Attack = new HowardAttackBehaviour();
            HowardAttackBehaviour.TimedStack[] Stack = new HowardAttackBehaviour.TimedStack[PGD.Count];

            for (int k = 0; k < Stack.Length; k++)
            {
                Stack[k] = new HowardAttackBehaviour.TimedStack
                {
                    Stack = StackList[PGD[k].StackNumber],
                    PreWaitTime = PGD[k].PreWait,
                    PostWaitTime = PGD[k].PostWait,
                    AnimationTriggerName = PGD[k].AnimationName
                };

                if (debug) MelonLogger.Msg(Stack[k].Stack.name);

                if (PGD[k].StackNumber == Stack_Straight || PGD[k].StackNumber == Stack_Explode || PGD[k].StackNumber == Stack_Kick || PGD[k].StackNumber == Stack_Uppercut || PGD[k].StackNumber == Stack_Parry || PGD[k].StackNumber == Stack_Stomp) { Stack[k].IsPersistentStack = true; }
                else { Stack[k].IsPersistentStack = false; }
            }

            Attack.name = SGD.AtkName;
            Attack.timedStacks = Stack;
            Attack.usePrediction = true;

            Timings[0] = new HowardSequence.HowardBehaviourTiming
            {
                Behaviour = Attack,
                PreActivationWaitTime = SGD.PreWait,
                PostActivationWaitTime = SGD.PostWait
            };

            Sequence.name = SGD.SeqName;
            Sequence.BehaviourTimings = Timings;

            Set.Sequence = Sequence;
            Set.Weight = SGD.Weight;
            Set.WeightDecrementationWhenSelected = SGD.WeightDec;
            Set.RequiredMinMaxRange = new Vector2(SGD.RangeMin, SGD.RangeMax);

            return Set;
        }

        //Interaction Panel
        public void ModifyHowardConsole()
        {
            #region Definitions
            GameObject OldCanvas, NewCanvas, OldText, Text, HowardConsole, BasePlate, UpperCanvas, LowerCanvas, OldPlate, LowerPlate, BaseButton, Button_Obj, Lamp;

            int BtAmnt = 3;
            float Offset = 0.3f;

            System.Collections.Generic.List<GameObject> Button = new System.Collections.Generic.List<GameObject>();
            #endregion

            #region Positions and Rotations
            Vector3 ConsolePos = new Vector3(-7.57f, 0.1f, -3.51f);

            Vector3 BasePlatePos = new Vector3(11.39f, 1.275f, 0.07f);
            Vector3 BasePlateRot = new Vector3(330f, 91f, 0f);
            Vector3 BasePlateScl = new Vector3(0.6f, 0.6f, 0.6f);

            Vector3 UpperCanvasPos = new Vector3(-0.12f, 0.12f,0.34f);
            Vector3 LowerCanvasPos = new Vector3(0, 0.04f, 0);
            Vector3 CanvasRot = new Vector3(90,0,0);

            Vector3 PlatePos = new Vector3(-0.6f, 0.07f, -0.55f);
            Vector3 PlateRot = new Vector3(0, 0, 270);
            Vector3 PlateScl = new Vector3(1, 0.7f, 0.5f);

            Vector3 ButtonSlabOffset = new Vector3(-0.4f, 0.105f, -0.53f);
            Vector3 ButtonBttnOffset = new Vector3(Offset, 0f, 0f);
            Vector3 ButtonRot = new Vector3(0f, 180f, 0f);
            Vector3 ButtonScl = new Vector3(1.25f, 1.25f, 1.25f);

            Vector3 TextPos = new Vector3(-0.4f, -0.73f, -0.07f);
            Vector3 TextRot = new Vector3(0f, 0f, 0f);

            Vector3 LampPos = new Vector3(0.5166f, 0.03f, 0.503f);
            Vector3 LampRot = new Vector3(90f, 0f, 0f);

            Vector3 OneScale = new Vector3(1, 1, 1);
            #endregion

            #region Object Find
            HowardConsole = GameObject.Find("--------------LOGIC--------------/Heinhouser products/Howard root/Howards console").gameObject;
            BasePlate = GameObject.Instantiate(GameObject.Find("--------------LOGIC--------------/Heinhouser products/MoveLearning/MoveLearnSelector/Model/Move selector"));
            OldCanvas = GameObject.Find("--------------LOGIC--------------/Slabbuddy menu variant/MenuForm/Base/ControlsSlab/GameSlabCanvas");
            OldPlate = GameObject.Find("--------------LOGIC--------------/Heinhouser products/Leaderboard/Text Objects/Titleplate/LeaderboardTitlePlate");

            BaseButton = GameObject.Find("------------TUTORIAL------------/Static tutorials/RUMBLE Starter Guide/Next Page Button/InteractionButton");
            Button_Obj = GameObject.Instantiate(BaseButton);
            Button_Obj.transform.GetChild(0).GetComponent<InteractionButton>().OnPressed.m_PersistentCalls.Clear();

            if (debug3) MelonLogger.Msg("Got Objects");
            #endregion

            #region Base Console
            HowardConsole.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
            HowardConsole.transform.GetChild(1).GetChild(4).gameObject.SetActive(false);
            HowardConsole.transform.localPosition = ConsolePos;
            if (debug3) MelonLogger.Msg("Did Stuff");
            #endregion

            #region Custom Base Plate
            BasePlate.name = "Base Plate Custom";
            BasePlate.transform.SetParent(HowardConsole.transform);
            BasePlate.transform.localPosition = BasePlatePos;
            BasePlate.transform.localEulerAngles = BasePlateRot;
            BasePlate.transform.localScale = BasePlateScl;
            if (debug3) MelonLogger.Msg("Did Stuff");
            #endregion

            #region Lower Plate
            LowerPlate = GameObject.Instantiate(OldPlate);
            LowerPlate.name = "Lower Plate";
            LowerPlate.transform.SetParent(BasePlate.transform);
            LowerPlate.transform.localPosition = PlatePos;
            LowerPlate.transform.localEulerAngles = PlateRot;
            LowerPlate.transform.localScale = PlateScl;
            #endregion

            #region Lamp
            Lamp = GameObject.Instantiate(GameObject.Find("--------------LOGIC--------------/Heinhouser products/Telephone 2.0 REDUX special edition/Notification Screen/NotificationLight"));
            Lamp.name = "Status Lamp";
            Lamp.transform.SetParent(BasePlate.transform);
            Lamp.transform.localPosition = LampPos;
            Lamp.transform.localEulerAngles = LampRot;
            Lamp.transform.GetComponent<MeshRenderer>().material = GameObject.Find("--------------LOGIC--------------/Heinhouser products/Howard root/DummyRoot/Howard/Dummy").GetComponent<SkinnedMeshRenderer>().material;
            Lamp.transform.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
            Lamp.transform.GetComponent<MeshRenderer>().material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            Lamp.transform.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.green);
            #endregion

            #region Canvas
            NewCanvas = GameObject.Instantiate(OldCanvas);
            NewCanvas.name = "SlabCanvas";
            NewCanvas.transform.SetParent(BasePlate.transform);
            if (debug3) MelonLogger.Msg("Did Stuff");
            #endregion

            #region Move Children to Canvas
            NewCanvas.transform.GetChild(0).GetChild(0).GetChild(1).name = "Text";
            NewCanvas.transform.GetChild(0).gameObject.SetActive(false);
            NewCanvas.SetActive(false);
            OldText = NewCanvas.transform.GetChild(0).GetChild(0).GetChild(1).gameObject;
            if (debug3) MelonLogger.Msg("Did Stuff");
            #endregion

            #region Canvas (Upper/Lower)
            UpperCanvas = GameObject.Instantiate(NewCanvas);
            LowerCanvas = GameObject.Instantiate(NewCanvas);
            UpperCanvas.SetActive(true);
            UpperCanvas.name = "Upper Canvas";
            UpperCanvas.transform.SetParent(BasePlate.transform);
            UpperCanvas.transform.localPosition = UpperCanvasPos;
            UpperCanvas.transform.localEulerAngles = CanvasRot;
            UpperCanvas.transform.localScale = OneScale;

            LowerCanvas.SetActive(true);
            LowerCanvas.name = "Lower Canvas";
            LowerCanvas.transform.SetParent(BasePlate.transform);
            LowerCanvas.transform.localPosition = LowerCanvasPos;
            LowerCanvas.transform.localEulerAngles = CanvasRot;
            LowerCanvas.transform.localScale = OneScale;
            if (debug3) MelonLogger.Msg("Did Stuff");
            #endregion

            #region Canvas (Upper) - Logic Text
            Text = GameObject.Instantiate(OldText);
            Text.name = "LogicText";
            Text.transform.SetParent(UpperCanvas.transform);
            Text.transform.localPosition = Vector3.zero;
            Text.transform.localEulerAngles = Vector3.zero;
            Text.transform.localScale = OneScale;
            Text.GetComponent<TextMeshProUGUI>().text = "Level 1";
            Text.GetComponent<TextMeshProUGUI>().fontSize = 0.18f;
            Text.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 0.24f);
            if (debug3) MelonLogger.Msg("Did Stuff");
            #endregion

            #region Canvas (Lower) - Nothing Rn
            Text = GameObject.Instantiate(OldText);
            Text.name = "LogicText";
            Text.transform.SetParent(LowerCanvas.transform);
            Text.transform.localPosition = Vector3.zero;
            Text.transform.localEulerAngles = Vector3.zero;
            Text.transform.localScale = OneScale;
            Text.GetComponent<TextMeshProUGUI>().text = "";
            Text.GetComponent<TextMeshProUGUI>().fontSize = 0.22f;
            Text.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 0.24f);
            if (debug3) MelonLogger.Msg("Did Stuff");
            #endregion

            for (int i = 0; i < BtAmnt; i++)
            {
                Button.Add(GameObject.Instantiate(Button_Obj));
                Button[i].name = "SlabButton" + (i + 1).ToString();
                Button[i].transform.SetParent(BasePlate.transform);
                Button[i].transform.localPosition = ButtonSlabOffset + (ButtonBttnOffset * (i));
                Button[i].transform.localEulerAngles = ButtonRot;
                Button[i].transform.localScale = ButtonScl;

                Text = GameObject.Instantiate(OldText);
                Text.transform.SetParent(LowerCanvas.transform);
                Text.name = "ButtonText" + (i + 1).ToString();
                Text.transform.localPosition = TextPos + (ButtonBttnOffset * (i));
                Text.transform.localEulerAngles = TextRot;
                Text.transform.localScale = OneScale;

                switch (i)
                {
                    case 0:
                        Text.GetComponent<TextMeshProUGUI>().text = "Prev";
                        Text.GetComponent<TextMeshProUGUI>().fontSize = 0.1f;
                        Button[i].transform.GetChild(0).GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                        {
                            ButtonHandler(0);
                        }));
                        break;
                    case 1:
                        Text.GetComponent<TextMeshProUGUI>().text = "Next";
                        Text.GetComponent<TextMeshProUGUI>().fontSize = 0.1f;
                        Button[i].transform.GetChild(0).GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                        {
                            ButtonHandler(1);
                        }));
                        break;
                    case 2:
                        Text.GetComponent<TextMeshProUGUI>().text = "Reload";
                        Text.GetComponent<TextMeshProUGUI>().fontSize = 0.1f;
                        Button[i].transform.GetChild(0).GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                        {
                            ButtonHandler(2);
                        }));
                        break;
                    default:
                        break;
                }

                Text.GetComponent<TextMeshProUGUI>().autoSizeTextContainer = true;
            }

            #region CleanUp
            GameObject.Destroy(NewCanvas);
            GameObject.Destroy(UpperCanvas.transform.GetChild(0).gameObject);
            GameObject.Destroy(LowerCanvas.transform.GetChild(0).gameObject);
            GameObject.Destroy(GameObject.Find("InteractionButton(Clone)"));
            GameObject.Destroy(GameObject.Find("Title(Clone)"));
            if (debug3) MelonLogger.Msg("Did Stuff");
            #endregion
        } 

        //Button Methods
        public void ButtonHandler(int Number)
        {
            if (ButtonDelay <= DateTime.Now)
            {
                switch (Number)
                {
                    case 0:
                        DecreaseLogicIndex();
                        ButtonDelay = DateTime.Now.AddSeconds(1);
                        break;
                    case 1:
                        IncreaseLogicIndex();
                        ButtonDelay = DateTime.Now.AddSeconds(1);
                        break;
                    case 2:
                        TriggerReload();
                        ButtonDelay = DateTime.Now.AddSeconds(3);
                        break;
                    default:
                        break;
                }
                if (debug2) MelonLogger.Msg("Button pressed");
            }
        }
        public void DecreaseLogicIndex()
        {
            if (currentlogicindex > 0)
            {
                currentlogicindex--;
                Howard_Obj.GetComponent<Howard>().SetCurrentLogicLevel(currentlogicindex);
                CurrentLogicName = LogicNameSanitization(Howard_Obj.GetComponent<Howard>().LogicLevels[currentlogicindex].name);
                LogicText_Obj.GetComponent<TextMeshProUGUI>().text = CurrentLogicName;
                MelonLogger.Msg("Howard Logic to " + CurrentLogicName);
            }
        }
        public void IncreaseLogicIndex()
        {
            if (currentlogicindex < (Howard_Obj.GetComponent<Howard>().LogicLevels.Count - 1))
            {
                currentlogicindex++;
                Howard_Obj.GetComponent<Howard>().SetCurrentLogicLevel(currentlogicindex);
                CurrentLogicName = LogicNameSanitization(Howard_Obj.GetComponent<Howard>().LogicLevels[currentlogicindex].name);
                LogicText_Obj.GetComponent<TextMeshProUGUI>().text = CurrentLogicName;
                MelonLogger.Msg("Howard Logic to " + CurrentLogicName);
            }
        }
        public void TriggerReload()
        {
            Howard_Obj.GetComponent<Howard>().SetHowardLogicActive(false);
            Howard_Obj.GetComponent<Howard>().SetCurrentLogicLevel(0);
            currentlogicindex = 0;
            CurrentLogicName = LogicNameSanitization(Howard_Obj.GetComponent<Howard>().LogicLevels[currentlogicindex].name);
            LogicText_Obj.GetComponent<TextMeshProUGUI>().text = CurrentLogicName;
            GetFromFile();
            MelonLogger.Msg("Refreshed Logic Files");
        }


        //Custom MovesetFiles
        public void GetFromFile()
        {
            bool SeqMarker = false;
            int LatestLogic;
            int NewLogicAmount;
            int OldLogicAmount;

            bool MovementEnable = false;
            float MovementWeight = 0;
            float MovementWeightDec = 0;
            float MovementSpeed = 0;
            float MovementMinAngle = 0;
            float MovementMaxAngle = 0;
            float DodgeSpeed = 0;
            float DodgeMinAngle = 0;
            float DodgeMaxAngle = 0;

            LogicData CustomLogic_Obj = new LogicData();
            SeqGenData SGD_Obj = new SeqGenData();
            PoseGenData PGD_Obj;
            HowardMoveBehaviour BaseMove;
            HowardMoveBehaviour DodgeMove;

            string[] Files;
            string[] Split = new string[2];
            string[] Colors = new string[4];

            System.Collections.Generic.List<HowardLogic> Logic_List = new System.Collections.Generic.List<HowardLogic>();
            System.Collections.Generic.List<PoseGenData> PGD_List = new System.Collections.Generic.List<PoseGenData>();
            Il2CppSystem.Collections.Generic.List<HowardLogic.SequenceSet> SeqSet_List = new Il2CppSystem.Collections.Generic.List<HowardLogic.SequenceSet>();


            Files = Directory.GetFiles(MoveSetPath);

            for (int i = 0; i < Files.Length; i++)
            {
                string[] Lines = File.ReadAllLines(Files[i]);
                if (debug) MelonLogger.Msg("Filename: " + Files[i]);
                if (debug) MelonLogger.Msg("FileLength: " + Lines.Length.ToString());

                for (int j = 0;j < Lines.Length; j++)
                {
                    if (!Lines[j].Contains("!"))
                    {
                        //Logic Setup
                        if (Lines[j].Contains("LogicName: "))
                        {
                            Split.Initialize();
                            Split = Lines[j].Split(':');
                            Split[1] = Split[1].Trim(' ');
                            CustomLogic_Obj.LogicName = Split[1];
                            if (debug) MelonLogger.Msg("Name: " + CustomLogic_Obj.LogicName);
                        }
                        if (Lines[j].Contains("MaxHP: "))
                        {
                            Split.Initialize();
                            Split = Lines[j].Split(':');
                            Split[1] = Split[1].Trim(' ');
                            CustomLogic_Obj.MaxHP = float.Parse(Split[1]);
                            if (debug) MelonLogger.Msg("MaxHP: " + CustomLogic_Obj.MaxHP.ToString());
                        }
                        if (Lines[j].Contains("DecisionMin: "))
                        {
                            Split.Initialize();
                            Split = Lines[j].Split(':');
                            Split[1] = Split[1].Trim(' ');
                            CustomLogic_Obj.DecisionMin = float.Parse(Split[1]);
                            if (debug) MelonLogger.Msg("DecisionMin: " + CustomLogic_Obj.DecisionMin.ToString());
                        }
                        if (Lines[j].Contains("DecisionMax: "))
                        {
                            Split.Initialize();
                            Split = Lines[j].Split(':');
                            Split[1] = Split[1].Trim(' ');
                            CustomLogic_Obj.DecisionMax = float.Parse(Split[1]);
                            if (debug) MelonLogger.Msg("DecisionMax: " + CustomLogic_Obj.DecisionMax.ToString());
                        }
                        if (Lines[j].Contains("ReactionTime: "))
                        {
                            Split.Initialize();
                            Split = Lines[j].Split(':');
                            Split[1] = Split[1].Trim(' ');
                            CustomLogic_Obj.ReactionTime = float.Parse(Split[1]);
                            if (debug) MelonLogger.Msg("ReactionTime: " + CustomLogic_Obj.ReactionTime.ToString());
                        }
                        //Colors
                        if (Lines[j].Contains("HeadLight: "))
                        {
                            Split.Initialize();
                            Colors.Initialize();
                            Split = Lines[j].Split(':');
                            Split[1] = Split[1].Trim(' ');
                            Colors = Split[1].Split(',');

                            CustomLogic_Obj.HeadLight = 
                                new Color(
                                float.Parse(Colors[0]) / 255,
                                float.Parse(Colors[1]) / 255,
                                float.Parse(Colors[2]) / 255,
                                float.Parse(Colors[3]) / 255
                                );
                            if (debug) MelonLogger.Msg("HeadLight: " + CustomLogic_Obj.HeadLight.ToString());
                        }
                        if (Lines[j].Contains("IdleColor: "))
                        {
                            Split.Initialize();
                            Colors.Initialize();
                            Split = Lines[j].Split(':');
                            Split[1] = Split[1].Trim(' ');
                            Colors = Split[1].Split(',');

                            CustomLogic_Obj.IdleColor =
                                new Color(
                                float.Parse(Colors[0]) / 255,
                                float.Parse(Colors[1]) / 255,
                                float.Parse(Colors[2]) / 255,
                                float.Parse(Colors[3]) / 255
                                );
                            if (debug) MelonLogger.Msg("IdleColor: " + CustomLogic_Obj.IdleColor.ToString());
                        }
                        if (Lines[j].Contains("ActiveColor: "))
                        {
                            Split.Initialize();
                            Colors.Initialize();
                            Split = Lines[j].Split(':');
                            Split[1] = Split[1].Trim(' ');
                            Colors = Split[1].Split(',');

                            CustomLogic_Obj.ActiveColor =
                                new Color(
                                float.Parse(Colors[0]) / 255,
                                float.Parse(Colors[1]) / 255,
                                float.Parse(Colors[2]) / 255,
                                float.Parse(Colors[3]) / 255
                                );
                            if (debug) MelonLogger.Msg("ActiveColor: " + CustomLogic_Obj.ActiveColor.ToString());
                        }
                        //Reaction Chances
                        if (Lines[j].Contains("DoMove: "))
                        {
                            Split.Initialize();
                            Split = Lines[j].Split(':');
                            Split[1] = Split[1].Trim(' ');
                            CustomLogic_Obj.Chance_DoSeq = float.Parse(Split[1]);
                            if (debug) MelonLogger.Msg("DoMove: " + CustomLogic_Obj.Chance_DoSeq.ToString());
                        }
                        if (Lines[j].Contains("Dodge: "))
                        {
                            Split.Initialize();
                            Split = Lines[j].Split(':');
                            Split[1] = Split[1].Trim(' ');
                            CustomLogic_Obj.Chance_Dodge = float.Parse(Split[1]);
                            if (debug) MelonLogger.Msg("Dodge: " + CustomLogic_Obj.Chance_Dodge.ToString());
                        }
                        if (Lines[j].Contains("DoNothing: "))
                        {
                            Split.Initialize();
                            Split = Lines[j].Split(':');
                            Split[1] = Split[1].Trim(' ');
                            CustomLogic_Obj.Chance_Nothing = float.Parse(Split[1]);
                            if (debug) MelonLogger.Msg("DoNothing: " + CustomLogic_Obj.Chance_Nothing.ToString());
                        }
                        //Moves / SequenceSets
                        if (Lines[j].Contains("Sequence - Init"))
                        {
                            SeqSet_List = new Il2CppSystem.Collections.Generic.List<HowardLogic.SequenceSet>();
                        }
                        if (Lines[j].Contains("Sequence - BaseMovement"))
                        {
                            Split = Lines[j + 1].Split(':');
                            MovementEnable = bool.Parse(Split[1].Trim(' '));
                            if (debug) MelonLogger.Msg("Movement Enable: " + MovementEnable.ToString());
                            if (MovementEnable)
                            {
                                Split = Lines[j + 2].Split(':');
                                MovementWeight = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("Movement Weight: " + MovementWeight.ToString());
                                Split = Lines[j + 3].Split(':');
                                MovementWeightDec = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("Movement WeightDec: " + MovementWeightDec.ToString());
                                Split = Lines[j + 4].Split(':');
                                MovementSpeed = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("Movement Speed: " + MovementSpeed.ToString());
                                Split = Lines[j + 5].Split(':');
                                MovementMinAngle = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("Movement Min Angle: " + MovementMinAngle.ToString());
                                Split = Lines[j + 6].Split(':');
                                MovementMaxAngle = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("Movement Max Angle: " + MovementMaxAngle.ToString());
                            }
                        }
                        if (Lines[j].Contains("Sequence - Dodge"))
                        {
                            Split = Lines[j + 1].Split(':');
                            DodgeSpeed = float.Parse(Split[1].Trim(' '));
                            if (debug) MelonLogger.Msg("Dodge Speed: " + DodgeSpeed.ToString());
                            Split = Lines[j + 2].Split(':');
                            DodgeMinAngle = float.Parse(Split[1].Trim(' '));
                            if (debug) MelonLogger.Msg("Dodge Min Angle: " + DodgeMinAngle.ToString());
                            Split = Lines[j + 3].Split(':');
                            DodgeMaxAngle = float.Parse(Split[1].Trim(' '));
                            if (debug) MelonLogger.Msg("Dodge Max Angle: " + DodgeMaxAngle.ToString());
                        }

                        if (Lines[j].Contains("Sequence - Start") && !SeqMarker)
                        {
                            SeqMarker = true;
                            PGD_List = new System.Collections.Generic.List<PoseGenData>();
                            if (debug) MelonLogger.Msg("Sequence - Start");
                        }
                        if (SeqMarker)
                        {
                            if (Lines[j].Contains("Sequence - Data"))
                            {
                                SGD_Obj = new SeqGenData();
                                Split.Initialize();
                                Split = Lines[j + 1].Split(':');
                                SGD_Obj.SeqName = Split[1].Trim(' ') + "Seq";
                                if (debug) MelonLogger.Msg("SeqName: " + SGD_Obj.SeqName);
                                SGD_Obj.AtkName = Split[1].Trim(' ') + "Atk";
                                if (debug) MelonLogger.Msg("AtkName: " + SGD_Obj.AtkName);

                                Split = Lines[j + 2].Split(':');
                                SGD_Obj.Weight = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("Weight: " + Split[1].Trim(' '));

                                Split = Lines[j + 3].Split(':');
                                SGD_Obj.WeightDec = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("Weight_Decrease: " + Split[1].Trim(' '));

                                Split = Lines[j + 4].Split(':');
                                SGD_Obj.PreWait = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("PreWait: " + Split[1].Trim(' '));

                                Split = Lines[j + 5].Split(':');
                                SGD_Obj.PostWait = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("PostWait: " + Split[1].Trim(' '));

                                Split = Lines[j + 6].Split(':');
                                SGD_Obj.RangeMin = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("RangeMin: " + Split[1].Trim(' '));

                                Split = Lines[j + 7].Split(':');
                                SGD_Obj.RangeMax = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("RangeMax: " + Split[1].Trim(' '));
                            }
                            if (Lines[j].Contains("Sequence - MoveData"))
                            {
                                PGD_Obj = new PoseGenData();

                                Split = Lines[j + 1].Split(':');
                                PGD_Obj.StackNumber = int.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("MoveNumber: " + PGD_Obj.StackNumber.ToString());
                                Split = Lines[j + 2].Split(':');
                                PGD_Obj.PreWait = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("PreWait: " + PGD_Obj.PreWait.ToString());
                                Split = Lines[j + 3].Split(':');
                                PGD_Obj.PostWait = float.Parse(Split[1].Trim(' '));
                                if (debug) MelonLogger.Msg("PostWait: " + PGD_Obj.PostWait.ToString());

                                //AnimationStuff
                                switch(PGD_Obj.StackNumber)
                                {
                                    case Stack_Kick:
                                        PGD_Obj.AnimationName = Anim_Kick;
                                        break;
                                    case Stack_Ball: 
                                    case Stack_Pillar: 
                                    case Stack_Cube: 
                                    case Stack_Wall:
                                        PGD_Obj.AnimationName = Anim_Spawn;
                                        break;
                                    default:
                                        PGD_Obj.AnimationName = Anim_Straight;
                                        break;
                                }
                                if (debug) MelonLogger.Msg("AnimName: " + PGD_Obj.AnimationName.ToString());

                                PGD_List.Add(PGD_Obj);
                            }
                        }
                        if (Lines[j].Contains("Sequence - End") && SeqMarker)
                        {
                            SeqMarker = false;
                            SeqSet_List.Add(CreatePoseSequence(PGD_List, SGD_Obj));
                            if (debug) MelonLogger.Msg("Sequence - End");
                        }
                        // File End
                        if (Lines[j].Contains("Logic - End"))
                        {
                            if (Logic_List.Count > 0)
                            {
                                foreach (var Logic in Logic_List)
                                {
                                    if (Logic.name == CustomLogic_Obj.LogicName)
                                    {
                                        CustomLogic_Obj.LogicName += "_";
                                    }
                                }
                            }
                            if (debug) MelonLogger.Msg("Got Duplicates");

                            Logic_List.Add(new HowardLogic
                            {
                                name = CustomLogic_Obj.LogicName,
                                maxHealth = CustomLogic_Obj.MaxHP,
                                MinMaxDecisionTime = new Vector2(CustomLogic_Obj.DecisionMin, CustomLogic_Obj.DecisionMax),
                                standStillReactiontime = CustomLogic_Obj.ReactionTime,
                                howardHeadlightColor = CustomLogic_Obj.HeadLight,
                                howardIdleLevelColor = CustomLogic_Obj.IdleColor,
                                howardLevelColor = CustomLogic_Obj.ActiveColor
                            });
                            if (debug) MelonLogger.Msg("Logic Init");

                            LatestLogic = Logic_List.Count - 1;
                            if (debug) MelonLogger.Msg("Logic Count");

                            DodgeMove = new HowardMoveBehaviour
                            {
                                name = "CustomDodge",
                                AnglePerSecond = DodgeSpeed,
                                minAngle = DodgeMinAngle,
                                maxAngle = DodgeMaxAngle,
                                negativeMoveAnimationTrigger = "RockSlideLeft",
                                positiveMoveAnimationTrigger = "RockSlideRight",
                                randomizeMovementSign = true
                            };

                            Logic_List[LatestLogic].DodgeBehaviour = DodgeMove;
                            if (debug) MelonLogger.Msg("Logic Dodge");

                            Logic_List[LatestLogic].reactions[0] = ModifyReactions(0, CustomLogic_Obj.Chance_DoSeq);        //ContinueActive
                            Logic_List[LatestLogic].reactions[1] = ModifyReactions(1, CustomLogic_Obj.Chance_Dodge);        //Dodge
                            Logic_List[LatestLogic].reactions[2] = ModifyReactions(2, CustomLogic_Obj.Chance_Nothing);      //DoNothing
                            if (debug) MelonLogger.Msg("Logic Reactions");

                            if (MovementEnable)
                            {
                                SeqSet_List.Add(new HowardLogic.SequenceSet
                                {
                                    RequiredMinMaxRange = new Vector2(0f, float.MaxValue),
                                    Weight = MovementWeight,
                                    WeightDecrementationWhenSelected = MovementWeightDec
                                });
                                if (debug) MelonLogger.Msg("1");
                                SeqSet_List[SeqSet_List.Count-1].Sequence = new HowardSequence
                                {
                                    name = "CustomMovement",
                                    BehaviourTimings = new HowardSequence.HowardBehaviourTiming[1]
                                };
                                if (debug) MelonLogger.Msg("2");
                                SeqSet_List[SeqSet_List.Count - 1].Sequence.BehaviourTimings[0] = new HowardSequence.HowardBehaviourTiming 
                                {
                                    PostActivationWaitTime = 0f,
                                    PreActivationWaitTime = 0.5f
                                };
                                if (debug) MelonLogger.Msg("3");
                                BaseMove = new HowardMoveBehaviour
                                {
                                    name = "CustomMovement",
                                    AnglePerSecond = MovementSpeed,
                                    minAngle = MovementMinAngle,
                                    maxAngle = MovementMaxAngle,
                                    negativeMoveAnimationTrigger = "RockSlideLeft",
                                    positiveMoveAnimationTrigger = "RockSlideRight",
                                    randomizeMovementSign = true
                                };
                                if (debug) MelonLogger.Msg("4");
                                SeqSet_List[SeqSet_List.Count - 1].Sequence.BehaviourTimings[0].Behaviour = BaseMove;
                                if (debug) MelonLogger.Msg("Logic Sequence Move");
                            }

                            Logic_List[LatestLogic].SequenceSets = SeqSet_List; 
                            if (debug) MelonLogger.Msg("Logic Sequence Custom");

                            if (debug) MelonLogger.Msg("Logic Applied");
                        }
                    }
                }
            }

            NewLogicAmount = Logic_List.Count;
            OldLogicAmount = Base_Logic_Array.Length;
            if (debug) MelonLogger.Msg("Get Length: " + NewLogicAmount.ToString() + "|" + OldLogicAmount.ToString());

            HowardLogic[] logicarray = new HowardLogic[(OldLogicAmount + NewLogicAmount)];
            if (debug) MelonLogger.Msg("Size Array");

            for (int i = 0; i < logicarray.Length; i++)
            {
                if (i < OldLogicAmount) logicarray[i] = Base_Logic_Array[i];
                if (i >= OldLogicAmount) logicarray[i] = Logic_List[i-OldLogicAmount];
                if (debug) MelonLogger.Msg("Loop: " + i.ToString());
            }
            if (debug) MelonLogger.Msg("Applied new logic");

            Howard_Obj.GetComponent<Howard>().LogicLevels = logicarray;
            if (debug) MelonLogger.Msg("Applied all logic");
        }

        //Basic Howard Manip
        private HowardLogic.ReactionChance ModifyReactions(int Index,float Input)
        {
            HowardLogic.ReactionChance temp = new HowardLogic.ReactionChance { Type = HowardLogic.ReactionType.DoNothing, Weight = 0f };
            switch (Index)
            {
                case 0:
                    temp.Type = HowardLogic.ReactionType.ContinueActive;
                    temp.Weight = Input;
                    break;
                case 1:
                    temp.Type = HowardLogic.ReactionType.Dodge;
                    temp.Weight = Input;
                    break;
                case 2:
                    temp.Type = HowardLogic.ReactionType.DoNothing;
                    temp.Weight = Input;
                    break;
            }
            return temp;
        }
        private string LogicNameSanitization(string Input)
        {
            string Output;
            float fontsize;
            switch (Input)
            {
                case "HowardLevel1":
                    Output = "Level 1";
                    break;
                case "HowardLevel2":
                    Output = "Level 2";
                    break;
                case "HowardLevel3":
                    Output = "Level 3";
                    break;
                default:
                    if (Input.Length == 0)
                    {
                        LogicText_Obj.GetComponent<TextMeshProUGUI>().fontSize = 0.18f;
                        Output = "No Name";
                    }
                    else if (Input.Length >= 12)
                    {
                        fontsize = ((float)Input.Length - 12) / 150;
                        LogicText_Obj.GetComponent<TextMeshProUGUI>().fontSize = 0.18f - fontsize;
                        Output = Input;
                    }
                    else
                    {
                        LogicText_Obj.GetComponent<TextMeshProUGUI>().fontSize = 0.18f;
                        Output = Input;
                    }
                    break;
            }
            return Output;
        }

        //Basic Functions
        public void LoadDelayLogic()
        {
            if (!loaddelaydone && !loadlockout)
            {
                loaddelay = DateTime.Now.AddSeconds(SceneDelay);
                loadlockout = true;
                if (debug) MelonLogger.Msg("LoadDelay: Start.");
                if (debug) MelonLogger.Msg(loaddelay.ToString());
            }
            if (DateTime.Now >= loaddelay && !loaddelaydone)
            {
                loaddelaydone = true;
                if (debug) MelonLogger.Msg("LoadDelay: End.");
            }
        }
        public void CheckandCreateFolder(string Input)
        {
            if (!Directory.Exists(Input))
            {
                Directory.CreateDirectory(Input);
                MelonLogger.Msg("Folder: " + Input.ToString() + " created.");
            }
        }


        //Overrides
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            currentscene = sceneName;
            loaddelaydone = false;
            loadlockout = false;
        }
    }
}

