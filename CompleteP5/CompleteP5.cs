using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using Satchel;
using UnityEngine;

namespace CompleteP5;

public class CompleteP5 : Mod
{
    internal static CompleteP5 Instance;
    private readonly string GGTHKScene = "GG_THK";
    private bool isTHKScene = false;

    public override string GetVersion() => AssemblyUtils.GetAssemblyVersionHash();
    
    public override List<(string, string)> GetPreloadNames()
    {
        return new List<(string, string)>
        {
            ("Room_Final_Boss_Core", "Boss Control"),
        };
    }

    private static GameObject THKGameObject;

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        if (Instance == null)
        {
            Instance = this;
            THKGameObject = preloadedObjects["Room_Final_Boss_Core"]["Boss Control"];
        }
        
        On.BossSequenceDoor.Start += AddBossesToPantheon;
        ModHooks.BeforeSceneLoadHook += CheckForCustomTHKScene;
        
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (_, _) =>
        {
            if (isTHKScene) ChangePVToTHK();
        };
    }

    private string CheckForCustomTHKScene(string nextScene)
    {
        if (nextScene == GGTHKScene)
        {
            isTHKScene = true;
            return "GG_Hollow_Knight";
        }
        
        isTHKScene = false;
        return nextScene;
    }

    private void AddBossesToPantheon(On.BossSequenceDoor.orig_Start orig, BossSequenceDoor self)
    {
        if (self.name == "GG_Final_Challenge_Door")
        {
            List<BossScene> P5Scenes = ReflectionHelper.GetField<BossSequence, BossScene[]>(self.bossSequence, "bossScenes").ToList();

            P5Scenes.Insert(51, CreateBossScene(GGTHKScene));
            P5Scenes.Insert(33, CreateBossScene("GG_Nosk_V"));

            ReflectionHelper.SetField(self.bossSequence, "bossScenes", P5Scenes.ToArray());

        }

        orig(self);
    }

    private BossScene CreateBossScene(string sceneName)
    {
        BossScene newBossScene = ScriptableObject.CreateInstance<BossScene>();
        newBossScene.sceneName = sceneName;
        newBossScene.isHidden = false;
        newBossScene.requireUnlock = false;
        return newBossScene;
    }
    
    private void ChangePVToTHK()
    {
        GameObject bossCtrl = Object.Instantiate(THKGameObject);
        bossCtrl.transform.Translate(10, 0, 0);
        bossCtrl.transform.Find("break_chains").Translate(-10, 0, 0);
        bossCtrl.transform.Find("Title").Translate(-10, 0, 0);
        bossCtrl.SetActive(true);

        PlayMakerFSM battleStart = bossCtrl.LocateMyFSM("Battle Start");
        battleStart.ChangeTransition("Init", "FINISHED", "Revisit");

        GameObject thk = bossCtrl.transform.Find("Hollow Knight Boss").gameObject;

        PlayMakerFSM control = thk.LocateMyFSM("Control");
        PlayMakerFSM phaseCtrl = thk.LocateMyFSM("Phase Control");

        BossSceneController bsc = BossSceneController.Instance;
        if (bsc.BossLevel >= 1)
        {
            thk.GetComponent<HealthManager>().hp = 1450;
            phaseCtrl.Fsm.GetFsmInt("Phase2 HP").Value = 870;
            phaseCtrl.Fsm.GetFsmInt("Phase3 HP").Value = 460;
        }

        control.RemoveFirstAction<PlayerDataBoolTest>("Long Roar End");
        phaseCtrl.RemoveFirstAction<PlayerDataBoolTest>("Set Phase 4");
        GameObject bossCorpse = thk.transform.Find("Boss Corpse").gameObject;
        PlayMakerFSM corpse = bossCorpse.LocateMyFSM("Corpse");
        corpse.RemoveFirstAction<SendEventByName>("Burst");
        corpse.AddCustomAction("Blow", () => bsc.EndBossScene());
        corpse.RemoveFirstAction<SetFsmBool>("Set Knight Focus");

        control.SetState("Init");

        GameObject battleScene = GameObject.Find("Battle Scene");
        GameObject godseeker = battleScene.transform.Find("Godseeker Crowd").gameObject;
        godseeker.transform.SetParent(null);
        FsmGameObject target = godseeker.LocateMyFSM("Control").Fsm.GetFsmGameObject("Target");
        target.Value = thk;
        battleStart.AddCustomAction("Roar Antic", () => target.Value = HeroController.instance.gameObject); 
        Object.Destroy(battleScene);
    }
}
