using UnityEngine;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using HollowTournament.Combat;
using HollowTournament.Player;
using HollowTournament.Enemies;
using HollowTournament.Boss;
using HollowTournament.Interactables;
using HollowTournament.CameraSystem;

namespace HollowTournament.EditorTools
{
    /// <summary>
    /// Builds a playable greybox version of the Arena level entirely from script,
    /// since Mixamo/Asset Store asset acquisition needs an interactive browser
    /// session this pipeline doesn't have. Primitive geometry + the procedural
    /// clips from AnimatorControllerBuilder stand in for final art; every
    /// gameplay system (FSMs, parry, kick-the-skull, boss phases) is fully wired
    /// and playable, so swapping in real models/animations later is a drop-in
    /// replacement, not a rebuild. Run via:
    /// Unity -batchmode -projectPath &lt;path&gt; -executeMethod HollowTournament.EditorTools.LevelBuilder.BuildGreyboxLevel -quit
    /// </summary>
    public static class LevelBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string PrefabFolder = "Assets/Prefabs";

        [MenuItem("Hollow Tournament/Build Greybox Level")]
        public static void BuildGreyboxLevel()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Give the scene a real asset path before anything is baked into it --
            // NavMeshBuilder.BuildNavMesh() silently fails to link its result to an
            // unsaved/untitled scene (the scene's NavMeshSettings.m_NavMeshData stays
            // fileID: 0), so every NavMeshAgent reports "no valid NavMesh" on reload.
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);

            var combatantController = AnimatorControllerBuilder.BuildCombatantController();
            var horsemanController = AnimatorControllerBuilder.BuildHorsemanController();
            var gateController = AnimatorControllerBuilder.BuildGateController();

            CreateLight();
            CreateGroundFloor();

            GameObject skullPrefab = CreateKickableSkullPrefab();
            GameObject bombPrefab = CreatePumpkinBombPrefab();
            GameObject headPrefab = CreatePumpkinHeadPrefab();

            GameObject player = CreatePlayer(new Vector3(0f, 0f, 0f), combatantController);

            CreateCamera(player);

            // --- Courtyard (teach Swing) ---
            CreateTrainingDummy("Dummy_A", new Vector3(14f, 0f, -3f));
            CreateTrainingDummy("Dummy_B", new Vector3(18f, 0f, 3f));

            // --- Gatehouse (teach Block) ---
            CreateSkeleton("Skeleton_Gatehouse_1", new Vector3(40f, 0f, -3f), SkeletonVariant.Basic, player, combatantController, skullPrefab);
            CreateSkeleton("Skeleton_Gatehouse_2", new Vector3(44f, 0f, 3f), SkeletonVariant.Basic, player, combatantController, skullPrefab);

            // --- Armory (teach Parry) ---
            CreateSkeleton("Skeleton_Armory_Armored", new Vector3(70f, 0f, 0f), SkeletonVariant.Armored, player, combatantController, skullPrefab);
            CreateWitch("Witch_Armory", new Vector3(76f, 0f, 5f), player, bombPrefab);

            // --- Chapel (teach Kick) ---
            GameObject shieldSkeleton = CreateSkeleton("Skeleton_Chapel_Shield", new Vector3(100f, 0f, 0f), SkeletonVariant.Shield, player, combatantController, skullPrefab);
            GameObject crowd1 = CreateSkeleton("Skeleton_Chapel_Crowd_1", new Vector3(106f, 0f, -4f), SkeletonVariant.Basic, player, combatantController, skullPrefab);
            GameObject crowd2 = CreateSkeleton("Skeleton_Chapel_Crowd_2", new Vector3(106f, 0f, 4f), SkeletonVariant.Basic, player, combatantController, skullPrefab);
            CreateChapelObjects(new Vector3(112f, 0f, 0f), gateController, new[] { crowd1, crowd2 });

            // --- Arena (Horseman boss) ---
            CreateHorseman(new Vector3(135f, 0f, 0f), player, horsemanController, headPrefab);

            BakeNavMesh();

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Hollow Tournament greybox level built at " + ScenePath);
        }

        // ---------- World ----------

        private static void CreateLight()
        {
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateGroundFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Ground_Floor";
            floor.transform.position = new Vector3(70f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(200f, 1f, 30f);
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);
        }

        /// <summary>
        /// com.unity.ai.navigation's NavMeshSurface would be the modern, build-safe
        /// choice, but every published version's Editor assembly fails to compile
        /// against this Unity build (Object.GetInstanceID() was made a hard
        /// obsolete-error, and the package hasn't caught up). Falling back to the
        /// legacy static bake; see BuildGreyboxLevel for why the scene must already
        /// have a saved path before this runs.
        /// </summary>
        private static void BakeNavMesh()
        {
            NavMeshBuilder.BuildNavMesh();
        }

        // ---------- Shared character scaffolding ----------

        private static GameObject CreateBodyRoot(string name, Vector3 pos, bool withCapsuleCollider)
        {
            var root = new GameObject(name);
            root.transform.position = pos;

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            if (withCapsuleCollider)
            {
                var col = root.AddComponent<CapsuleCollider>();
                col.height = 2f;
                col.radius = 0.5f;
                col.center = new Vector3(0f, 1f, 0f);
            }

            var model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "Model";
            model.transform.SetParent(root.transform);
            model.transform.localPosition = new Vector3(0f, 1f, 0f);
            Object.DestroyImmediate(model.GetComponent<Collider>());

            return root;
        }

        private static WeaponHitbox CreateWeaponHitbox(Transform parent, string name, Vector3 localOffset)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent);
            pivot.transform.localPosition = new Vector3(0f, 1f, 0f);

            var hitboxGO = new GameObject("Hitbox");
            hitboxGO.transform.SetParent(pivot.transform);
            hitboxGO.transform.localPosition = localOffset;

            var box = hitboxGO.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.4f, 0.4f, 1.2f);

            return hitboxGO.AddComponent<WeaponHitbox>();
        }

        // ---------- Player ----------

        private static GameObject CreatePlayer(Vector3 pos, AnimatorController controller)
        {
            var root = new GameObject("Player");
            root.transform.position = pos;
            root.tag = "Player";

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var cc = root.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0f, 1f, 0f);

            var model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "Model";
            model.transform.SetParent(root.transform);
            model.transform.localPosition = new Vector3(0f, 1f, 0f);
            Object.DestroyImmediate(model.GetComponent<Collider>());

            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            var weaponHitbox = CreateWeaponHitbox(root.transform, "WeaponPivot", new Vector3(0f, 0f, 1f));

            var swingReader = root.AddComponent<SwingDirectionReader>();
            var relay = root.AddComponent<AnimationEventRelay>();
            SetPrivateField(relay, "hitbox", weaponHitbox);

            var lockOn = root.AddComponent<LockOnCamera>();
            lockOn.SetPlayer(root.transform);

            var controller2 = root.AddComponent<PlayerController>();
            controller2.animator = animator;
            controller2.weaponHitbox = weaponHitbox;
            controller2.swingReader = swingReader;
            controller2.lockOnCamera = lockOn;

            return root;
        }

        private static void CreateCamera(GameObject player)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.transform.position = player.transform.position + new Vector3(0f, 2.2f, -6f);
            camGO.AddComponent<AudioListener>();
            var cam = camGO.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;

            var rig = camGO.AddComponent<ThirdPersonFollowCamera>();
            SetPrivateField(rig, "target", player.transform);
            SetPrivateField(rig, "lockOn", player.GetComponent<LockOnCamera>());
        }

        // ---------- Enemies ----------

        private static void CreateTrainingDummy(string name, Vector3 pos)
        {
            var root = CreateBodyRoot(name, pos, true);
            root.AddComponent<TrainingDummy>();
        }

        private static GameObject CreateSkeleton(string name, Vector3 pos, SkeletonVariant variant, GameObject player, AnimatorController controller, GameObject skullPrefab)
        {
            var root = CreateBodyRoot(name, pos, true);

            var agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.speed = 3.2f;
            agent.acceleration = 12f;

            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            var weaponHitbox = CreateWeaponHitbox(root.transform, "WeaponPivot", new Vector3(0f, 0f, 1f));
            var relay = root.AddComponent<AnimationEventRelay>();
            SetPrivateField(relay, "hitbox", weaponHitbox);

            var skeleton = root.AddComponent<SkeletonEnemy>();
            skeleton.variant = variant;
            skeleton.animator = animator;
            skeleton.weaponHitbox = weaponHitbox;
            skeleton.target = player.transform;

            var dropper = root.AddComponent<SkullDropper>();
            SetPrivateField(dropper, "skullPrefab", skullPrefab);

            return root;
        }

        private static void CreateWitch(string name, Vector3 pos, GameObject player, GameObject bombPrefab)
        {
            var root = CreateBodyRoot(name, pos, true);

            var throwOrigin = new GameObject("ThrowOrigin");
            throwOrigin.transform.SetParent(root.transform);
            throwOrigin.transform.localPosition = new Vector3(0f, 1.3f, 0.5f);

            var witch = root.AddComponent<WitchEnemy>();
            SetPrivateField(witch, "pumpkinBombPrefab", bombPrefab);
            SetPrivateField(witch, "throwOrigin", throwOrigin.transform);
            SetPrivateField(witch, "player", player.transform);
        }

        private static void CreateChapelObjects(Vector3 gatePos, AnimatorController gateController, GameObject[] crowd)
        {
            var gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = "Chapel_Gate";
            gate.transform.position = gatePos + new Vector3(0f, 1.5f, 0f);
            gate.transform.localScale = new Vector3(4f, 3f, 0.5f);
            var gateAnimator = gate.AddComponent<Animator>();
            gateAnimator.runtimeAnimatorController = gateController;

            var bellGO = new GameObject("Chapel_Bell");
            bellGO.transform.position = gatePos + new Vector3(-6f, 1.5f, 0f);
            var bellCollider = bellGO.AddComponent<SphereCollider>();
            bellCollider.radius = 0.6f;
            var bellRb = bellGO.AddComponent<Rigidbody>();
            bellRb.isKinematic = true;
            bellRb.useGravity = false;
            var bell = bellGO.AddComponent<BellTrigger>();
            SetPrivateField(bell, "gateAnimator", gateAnimator);

            var zoneGO = new GameObject("Chapel_CrowdZone");
            zoneGO.transform.position = gatePos + new Vector3(-6f, 1f, 0f);
            var zoneCollider = zoneGO.AddComponent<BoxCollider>();
            zoneCollider.isTrigger = true;
            zoneCollider.size = new Vector3(14f, 3f, 12f);
            zoneGO.AddComponent<CrowdStaggerZone>();
            // skeletonsInZone populates itself via trigger overlap at runtime once the crowd walks in.
        }

        private static void CreateHorseman(Vector3 pos, GameObject player, AnimatorController horsemanController, GameObject headPrefab)
        {
            var root = CreateBodyRoot("Horseman", pos, true);

            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = horsemanController;

            var lanceHitbox = CreateWeaponHitbox(root.transform, "LancePivot", new Vector3(0f, 0f, 2f));
            var meleeHitbox = CreateWeaponHitbox(root.transform, "MeleePivot", new Vector3(0f, 0f, 1f));

            var chargeStart = new GameObject("ChargeStart");
            chargeStart.transform.position = pos + new Vector3(0f, 0f, -12f);
            var chargeEnd = new GameObject("ChargeEnd");
            chargeEnd.transform.position = pos + new Vector3(0f, 0f, 12f);

            var headOrigin = new GameObject("HeadThrowOrigin");
            headOrigin.transform.SetParent(root.transform);
            headOrigin.transform.localPosition = new Vector3(0f, 1.6f, 0.5f);

            var boss = root.AddComponent<HorsemanBoss>();
            boss.animator = animator;
            boss.lanceHitbox = lanceHitbox;
            boss.meleeHitbox = meleeHitbox;
            boss.player = player.transform;
            boss.chargeStartPoint = chargeStart.transform;
            boss.chargeEndPoint = chargeEnd.transform;
            boss.headThrowOrigin = headOrigin.transform;
            boss.pumpkinHeadProjectilePrefab = headPrefab;
        }

        // ---------- Prefabs ----------

        private static GameObject CreateKickableSkullPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "KickableSkull";
            go.transform.localScale = Vector3.one * 0.4f;
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = true;
            go.AddComponent<KickableSkull>();
            return SaveAndDestroyTemp(go, "KickableSkull");
        }

        private static GameObject CreatePumpkinBombPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PumpkinBomb";
            go.transform.localScale = Vector3.one * 0.4f;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            go.AddComponent<PumpkinBomb>();
            return SaveAndDestroyTemp(go, "PumpkinBomb");
        }

        private static GameObject CreatePumpkinHeadPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PumpkinHeadProjectile";
            go.transform.localScale = Vector3.one * 0.5f;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            go.AddComponent<PumpkinHeadProjectile>();
            return SaveAndDestroyTemp(go, "PumpkinHeadProjectile");
        }

        private static GameObject SaveAndDestroyTemp(GameObject instance, string name)
        {
            System.IO.Directory.CreateDirectory(PrefabFolder);
            string path = $"{PrefabFolder}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        // ---------- Reflection helper for private [SerializeField] refs ----------

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field == null)
                throw new System.Exception($"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }
}
