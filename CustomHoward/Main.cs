using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using RUMBLE.Environment.Howard;
using RUMBLE.MoveSystem;
using System.CodeDom;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using RUMBLE.Interactions.InteractionBase;
using TMPro;
using Il2CppSystem.Xml.Schema;

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
        private const double SceneDelay = 6.0;
        private const double RefreshDelay = 3.0;
        private const string BaseFolder = "UserData";
        private const string ModFolder = "CustomHoward";
        private const string MoveFolder = "CustomMoveSet";

        //constants - Stack Numbers
        private const int Stack_Explode = 0;
        private const int Stack_Flick = 1;
        private const int Stack_HoldR = 2;
        private const int Stack_Parry = 3;
        private const int Stack_HoldL = 4;
        private const int Stack_Cube = 5;
        private const int Stack_Dash = 6;
        private const int Stack_Uppercut = 7;
        private const int Stack_Wall = 8;
        private const int Stack_Jump = 9;
        private const int Stack_Kick = 10;
        private const int Stack_Ball = 11;
        private const int Stack_Stomp = 12;
        private const int Stack_Pillar = 13;
        private const int Stack_Straight = 14;
        private const int Stack_Disc = 15;

        //constants - animations
        private const string Anim_Straight = "Straight";
        private const string Anim_Kick = "Kick";
        private const string Anim_Spawn = "SpawnStructure";

        //variables
        private bool debug = false;
        private bool debug2 = false;
        private bool HActive = false;
        private bool PrevActive = false;
        private bool loaddelaydone = false;
        private bool loadlockout = false;
        private bool dorefresh = false;
        private bool doreactivate = false;

        private int currentlogicindex = 0;

        private string currentscene;
        private string MoveSetPath;
        private string CurrentLogicName;

        private DateTime loaddelay;
        private DateTime logicRefreshDelay;
        private DateTime ButtonDelay = DateTime.Now;
        private DateTime ReactivateDelay;

        //objects/collections
        private GameObject Howard_Obj;
        private HowardLogic[] Base_Logic_Array = new HowardLogic[3];
        private Il2CppSystem.Collections.Generic.List<Stack> StackList;
        private GameObject DebugPlayer;
        private GameObject LogicText_Obj;
        private GameObject ActiveText_Obj;
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


                    CreateControlSlab();

                    LogicText_Obj = GameObject.Find("HowardSlab/SlabCanvas/LogicText");
                    ActiveText_Obj = GameObject.Find("HowardSlab/SlabCanvas/HowardActiveText");
                    Lamp_Obj = GameObject.Find("HowardSlab/SlabLamp");
                }
                else
                {
                    //Buttons
                    if (Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        ButtonHandler(2);
                    }
                    if (Input.GetKeyDown(KeyCode.KeypadPlus))
                    {
                        ButtonHandler(1);
                    }
                    if (Input.GetKeyDown(KeyCode.KeypadMinus))
                    {
                        ButtonHandler(0);
                    }
                    if (Input.GetKeyDown(KeyCode.Keypad0) && !dorefresh)
                    {
                        ButtonHandler(5);
                    }

                    //DelayLogic
                    if (dorefresh && DateTime.Now >= logicRefreshDelay)
                    {
                        Howard_Obj.GetComponent<Howard>().SetCurrentLogicLevel(currentlogicindex);
                        CurrentLogicName = Howard_Obj.GetComponent<Howard>().LogicLevels[currentlogicindex].name;
                        MelonLogger.Msg("Howard Logic to " + CurrentLogicName);
                        if (HActive) Howard_Obj.GetComponent<Howard>().SetHowardLogicActive(HActive);
                        dorefresh = false;
                    }
                    if(doreactivate && DateTime.Now >= ReactivateDelay)
                    {
                        Howard_Obj.GetComponent<Howard>().SetHowardLogicActive(HActive);
                        MelonLogger.Msg("Howard Reactivated after Logic Change");
                        doreactivate = false;
                    }

                    if (HActive != PrevActive)
                    {
                        if (HActive) 
                        { 
                            ActiveText_Obj.GetComponent<TextMeshProUGUI>().text = "Yes";
                            Lamp_Obj.transform.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.green);
                        }
                        else 
                        { 
                            ActiveText_Obj.GetComponent<TextMeshProUGUI>().text = "No"; 
                            Lamp_Obj.transform.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.red);
                        }
                        PrevActive = HActive;
                    }

                    //DEBUG
                    if (Input.GetKeyDown(KeyCode.KeypadDivide))
                    {
                        DebugPlayer = GameObject.Find("Player Controller(Clone)");
                        DebugPlayer.transform.position = new Vector3(4f, 0.5f, -20f);
                        DebugPlayer.transform.eulerAngles = new Vector3(0, 75, 0);
                        MelonLogger.Msg("Player Moved");
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

        //Interaction Slab
        public void CreateControlSlab()
        {
            #region GameObjects
            GameObject temp;
            GameObject Text;
            GameObject Button_Obj;
            GameObject OldSlab;
            GameObject NewSlab;
            GameObject OldCanvas;
            GameObject NewCanvas;
            GameObject BaseButton;
            GameObject Divider_Obj;
            GameObject Lamp;

            System.Collections.Generic.List<GameObject> Button = new System.Collections.Generic.List<GameObject>();
            #endregion

            #region Variables / Constants
            int BtAmnt = 6;
            float Offset = 0.2f;

            Vector3 SlabPos = new Vector3(-1f, - 2f, - 25f);
            Vector3 SlabRot = new Vector3(0f, 90f, 0f);

            Vector3 ButtonSlabOffset = new Vector3(0.8f, -0.2f, 0.055f);
            Vector3 ButtonBttnOffset = new Vector3(-Offset, 0f, 0f);
            Vector3 ButtonRot = new Vector3(270f, 180f, 0f);

            Vector3 TextPos = new Vector3(-0.57f, -0.4f, 0f);
            Vector3 TextRot = new Vector3(0f, 0f, 0f);

            Vector3 CanvasPos = new Vector3(0.03f, 0.3f, 0.056f);
            Vector3 CanvasRot = new Vector3(0f, 180f, 0f);

            Vector3 LampPos = new Vector3(-0.45f, 1f, 0f);
            Vector3 LampRot = new Vector3(30f, 0f, 45f);

            Vector3 TitlePos = new Vector3(0f, 0.5f, 0f);
            Vector3 DividerBelowTitle = new Vector3(0f, 0.5002f, 0f);
            Vector3 ActiveLogicTextPos = new Vector3(0, 0.34f, 0f);
            Vector3 LogicTextPos = new Vector3(0, 0.24f, 0f);
            Vector3 DividerBelowLogicText = new Vector3(0f, 0.24f, 0f);
            Vector3 IsHowardActiveText = new Vector3(0, 0.08f, 0f);
            Vector3 HowardActiveText = new Vector3(0, -0.02f, 0f);
            Vector3 DividerBelowActiveText = new Vector3(0f, -0.02f, 0f);
            Vector3 DividerAboveButtonText = new Vector3(-0.08f, -0.3f, 0f);
            Vector3 DividerBelowButtonText = new Vector3(-0.08f, -0.4f, 0f);
            #endregion

            #region Copy Objects
            OldSlab = GameObject.Find("--------------LOGIC--------------/Slabbuddy menu variant/MenuForm/Base/Base Mesh");
            OldCanvas = GameObject.Find("--------------LOGIC--------------/Slabbuddy menu variant/MenuForm/Base/ControlsSlab/GameSlabCanvas");
            BaseButton = GameObject.Find("------------TUTORIAL------------/Static tutorials/RUMBLE Starter Guide/Next Page Button/InteractionButton");
            Button_Obj = GameObject.Instantiate(BaseButton);
            Button_Obj.transform.GetChild(0).GetComponent<InteractionButton>().OnPressed.m_PersistentCalls.Clear();
            #endregion

            #region Slab
            NewSlab = GameObject.Instantiate(OldSlab);
            NewSlab.name = "HowardSlab";
            NewSlab.transform.position = SlabPos;
            NewSlab.transform.eulerAngles = SlabRot;
            NewSlab.transform.GetChild(0).name = "SlabMesh";
            #endregion

            #region Lamp
            Lamp = GameObject.Instantiate(GameObject.Find("--------------LOGIC--------------/Heinhouser products/Telephone 2.0 REDUX special edition/Notification Screen/NotificationLight"));
            Lamp.name = "SlabLamp";
            Lamp.transform.SetParent(NewSlab.transform);
            Lamp.transform.localPosition = LampPos;
            Lamp.transform.localEulerAngles = LampRot;
            Lamp.transform.GetComponent<MeshRenderer>().material = GameObject.Find("--------------LOGIC--------------/Heinhouser products/Howard root/DummyRoot/Howard/Dummy").GetComponent<SkinnedMeshRenderer>().material;
            Lamp.transform.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
            Lamp.transform.GetComponent<MeshRenderer>().material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            Lamp.transform.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.red);
            #endregion

            #region Canvas
            NewCanvas = GameObject.Instantiate(OldCanvas);
            NewCanvas.name = "SlabCanvas";
            NewCanvas.transform.SetParent(NewSlab.transform);
            NewCanvas.transform.localPosition = CanvasPos;
            NewCanvas.transform.localEulerAngles = CanvasRot;
            #endregion

            #region Move Children to Canvas
            NewCanvas.transform.GetChild(0).GetChild(0).GetChild(1).name = "Title";
            NewCanvas.transform.GetChild(0).GetChild(0).GetChild(2).name = "DividerBelowTitle";
            NewCanvas.transform.GetChild(0).GetChild(0).GetChild(3).name = "BaseDivider";
            NewCanvas.transform.GetChild(0).GetChild(0).GetChild(3).SetParent(NewCanvas.transform);
            NewCanvas.transform.GetChild(0).GetChild(0).GetChild(2).SetParent(NewCanvas.transform);
            NewCanvas.transform.GetChild(0).GetChild(0).GetChild(1).SetParent(NewCanvas.transform);
            #endregion

            temp = GameObject.Instantiate(NewCanvas.transform.GetChild(3).gameObject);

            NewCanvas.transform.FindChild("Title").localPosition = TitlePos;
            NewCanvas.transform.FindChild("Title").GetComponent<TextMeshProUGUI>().text = "Logic Controller";
            NewCanvas.transform.FindChild("Title").GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 0.17f);
            NewCanvas.transform.FindChild("DividerBelowTitle").localPosition = DividerBelowTitle;

            #region DividerBelowButtonText
            Divider_Obj = GameObject.Instantiate(NewCanvas.transform.FindChild("BaseDivider").gameObject);
            Divider_Obj.transform.SetParent(NewCanvas.transform);
            Divider_Obj.name = "DividerBelowButtonText";
            Divider_Obj.transform.localPosition = DividerBelowButtonText;
            Divider_Obj.transform.localEulerAngles = TextRot;
            #endregion

            #region DividerAboveButtonText
            Divider_Obj = GameObject.Instantiate(NewCanvas.transform.FindChild("BaseDivider").gameObject);
            Divider_Obj.transform.SetParent(NewCanvas.transform);
            Divider_Obj.name = "DividerAboveButtonText";
            Divider_Obj.transform.localPosition = DividerAboveButtonText;
            Divider_Obj.transform.localEulerAngles = TextRot;
            #endregion

            #region DividerBelowLogicText
            Divider_Obj = GameObject.Instantiate(NewCanvas.transform.FindChild("DividerBelowTitle").gameObject);
            Divider_Obj.transform.SetParent(NewCanvas.transform);
            Divider_Obj.name = "DividerBelowLogicText";
            Divider_Obj.transform.localPosition = DividerBelowLogicText;
            Divider_Obj.transform.localEulerAngles = TextRot;
            #endregion

            #region DividerBelowActiveText
            Divider_Obj = GameObject.Instantiate(NewCanvas.transform.FindChild("DividerBelowTitle").gameObject);
            Divider_Obj.transform.SetParent(NewCanvas.transform);
            Divider_Obj.name = "DividerBelowActiveText";
            Divider_Obj.transform.localPosition = DividerBelowActiveText;
            Divider_Obj.transform.localEulerAngles = TextRot;
            #endregion

            #region LogicTextHeader
            Text = GameObject.Instantiate(temp);
            Text.transform.SetParent(NewCanvas.transform);
            Text.name = "ActiveLogicText";
            Text.transform.localPosition = ActiveLogicTextPos;
            Text.transform.localEulerAngles = TextRot;
            Text.GetComponent<TextMeshProUGUI>().text = "Active Logic:";
            Text.GetComponent<TextMeshProUGUI>().fontSize = 0.12f;
            Text.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 0.15f);
            #endregion

            #region LogicText
            Text = GameObject.Instantiate(temp);
            Text.transform.SetParent(NewCanvas.transform);
            Text.name = "LogicText";
            Text.transform.localPosition = LogicTextPos;
            Text.transform.localEulerAngles = TextRot;
            Text.GetComponent<TextMeshProUGUI>().text = Howard_Obj.GetComponent<Howard>().LogicLevels[currentlogicindex].name;
            Text.GetComponent<TextMeshProUGUI>().fontSize = 0.12f;
            Text.GetComponent<RectTransform>().sizeDelta = new Vector2(1f,0.15f);
            #endregion

            #region IsHowardActiveText
            Text = GameObject.Instantiate(temp);
            Text.transform.SetParent(NewCanvas.transform);
            Text.name = "IsHowardActiveText";
            Text.transform.localPosition = IsHowardActiveText;
            Text.transform.localEulerAngles = TextRot;
            Text.GetComponent<TextMeshProUGUI>().text = "Is Howard Active ?";
            Text.GetComponent<TextMeshProUGUI>().fontSize = 0.12f;
            Text.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 0.15f);
            #endregion

            #region HowardActiveText
            Text = GameObject.Instantiate(temp);
            Text.transform.SetParent(NewCanvas.transform);
            Text.name = "HowardActiveText";
            Text.transform.localPosition = HowardActiveText;
            Text.transform.localEulerAngles = TextRot;
            Text.GetComponent<TextMeshProUGUI>().text = "No";
            Text.GetComponent<TextMeshProUGUI>().fontSize = 0.12f;
            Text.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 0.15f);
            #endregion


            for (int i = 0; i < BtAmnt; i++) 
            {
                Button.Add(GameObject.Instantiate(Button_Obj));
                Button[i].name = "SlabButton" + (i + 1).ToString();
                Button[i].transform.SetParent(NewSlab.transform);
                Button[i].transform.localPosition = ButtonSlabOffset + (ButtonBttnOffset * (i + 1));
                Button[i].transform.localEulerAngles = ButtonRot;

                Text = GameObject.Instantiate(temp);
                Text.transform.SetParent(NewCanvas.transform);
                Text.name = "ButtonText" + (i + 1).ToString();
                Text.transform.localPosition = TextPos + (-ButtonBttnOffset * (i));
                Text.transform.localEulerAngles = TextRot;

                switch (i)
                {
                    case 0:
                        Text.GetComponent<TextMeshProUGUI>().text = "Prev";
                        Text.GetComponent<TextMeshProUGUI>().fontSize = 0.08f;
                        Button[i].transform.GetChild(0).GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                        {
                            ButtonHandler(0);
                        }));
                        break;
                    case 1:
                        Text.GetComponent<TextMeshProUGUI>().text = "Next";
                        Text.GetComponent<TextMeshProUGUI>().fontSize = 0.08f;
                        Button[i].transform.GetChild(0).GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                        {
                            ButtonHandler(1);
                        }));
                        break;
                    case 2:
                        Text.GetComponent<TextMeshProUGUI>().text = "On/Off";
                        Text.GetComponent<TextMeshProUGUI>().fontSize = 0.08f;
                        Button[i].transform.GetChild(0).GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                        {
                            ButtonHandler(2);
                        }));
                        break;
                    case 5:
                        Text.GetComponent<TextMeshProUGUI>().text = "Reload";
                        Text.GetComponent<TextMeshProUGUI>().fontSize = 0.08f;
                        Button[i].transform.GetChild(0).GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                        {
                            ButtonHandler(5);
                        }));
                        break;
                    default:
                        Text.GetComponent<TextMeshProUGUI>().text = "X";
                        Text.GetComponent<TextMeshProUGUI>().fontSize = 0.08f;
                        break;
                }

                Text.GetComponent<TextMeshProUGUI>().autoSizeTextContainer = true;
            }

            NewCanvas.transform.FindChild("BaseDivider").gameObject.SetActive(false);

            GameObject.Destroy(NewCanvas.transform.GetChild(0).gameObject);
            GameObject.Destroy(GameObject.Find("InteractionButton(Clone)"));
            GameObject.Destroy(GameObject.Find("Title(Clone)"));
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
                        ToggleHoward();
                        ButtonDelay = DateTime.Now.AddSeconds(3);
                        break;
                    case 5:
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
                CurrentLogicName = Howard_Obj.GetComponent<Howard>().LogicLevels[currentlogicindex].name;
                LogicText_Obj.GetComponent<TextMeshProUGUI>().text = CurrentLogicName;
                MelonLogger.Msg("Howard Logic to " + CurrentLogicName);
                if (HActive)
                {
                    ActiveText_Obj.GetComponent<TextMeshProUGUI>().text = "No";
                    doreactivate = true;
                    ReactivateDelay = DateTime.Now.AddSeconds(4);
                }
            }
        }
        public void IncreaseLogicIndex()
        {
            if (currentlogicindex < (Howard_Obj.GetComponent<Howard>().LogicLevels.Count - 1))
            {
                currentlogicindex++;
                Howard_Obj.GetComponent<Howard>().SetCurrentLogicLevel(currentlogicindex);
                CurrentLogicName = Howard_Obj.GetComponent<Howard>().LogicLevels[currentlogicindex].name;
                LogicText_Obj.GetComponent<TextMeshProUGUI>().text = CurrentLogicName;
                MelonLogger.Msg("Howard Logic to " + CurrentLogicName);
                if (HActive)
                {
                    ActiveText_Obj.GetComponent<TextMeshProUGUI>().text = "No";
                    doreactivate = true;
                    ReactivateDelay = DateTime.Now.AddSeconds(4);
                }
            }
        }
        public void ToggleHoward()
        {
            HActive = !HActive;
            Howard_Obj.GetComponent<Howard>().SetHowardLogicActive(HActive);
            if (HActive) { ActiveText_Obj.GetComponent<TextMeshProUGUI>().text = "Yes"; }
            else { ActiveText_Obj.GetComponent<TextMeshProUGUI>().text = "No"; }
            MelonLogger.Msg("Howard State: " + HActive.ToString());
        }
        public void TriggerReload()
        {
            Howard_Obj.GetComponent<Howard>().SetHowardLogicActive(false);
            Howard_Obj.GetComponent<Howard>().SetCurrentLogicLevel(0);
            GetFromFile();
            MelonLogger.Msg("Refreshed Logic Files");
            logicRefreshDelay = DateTime.Now.AddSeconds(RefreshDelay);
            dorefresh = true;
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
        public HowardLogic.ReactionChance ModifyReactions(int Index,float Input)
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

