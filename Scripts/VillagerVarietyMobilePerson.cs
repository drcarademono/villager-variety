// Project:         Villager Variety mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2021 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Authors:         Hazelnut & Carademono

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Utility.AssetInjection;

// NPC Sprite images should be named like this:
// archive.face.variant_record-frame.png
// e.g. 123.145.1w_3-1.png
// variant is a number followed (optionally) by a season letter
// season is p (spring), m (summer), f (fall), w (winter)  [use one as the default, can be different per sprite]

namespace VillagerVariety
{
    // Copied from DFU MobilePersonBillboard class and modified.
    [ImportedComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VillagerVarietyMobilePerson : MobilePersonAsset
    {
        private const string EMISSION = "_Emission";
        private const string EMISSIONMAP = "_EmissionMap";
        private const string EMISSIONCOLOR = "_EmissionColor";

        private static Dictionary<string, Texture2D[][]> textureCache = new Dictionary<string, Texture2D[][]>();
        private static Dictionary<string, Texture2D[][]> emmisionCache = new Dictionary<string, Texture2D[][]>();

        #region Fields

        const int numberOrientations = 8;
        const float anglePerOrientation = 360f / numberOrientations;

        Vector3 cameraPosition;
        Camera mainCamera = null;
        MeshFilter meshFilter = null;
        MeshRenderer meshRenderer = null;
        float facingAngle;
        int lastOrientation;
        AnimStates currentAnimState;

        Vector2[] recordSizes;
        int[] recordFrames;
        Rect[] atlasRects;
        RecordIndex[] atlasIndices;
        MobileAnimation[] stateAnims;
        MobileAnimation[] moveAnims;
        MobileAnimation[] idleAnims;
        MobileBillboardImportedTextures importedTextures;
        int currentFrame = 0;

        float animSpeed;
        float animTimer = 0;

        bool isUsingGuardTexture = false;

        #endregion

        #region Textures

        int[] maleRedguardTextures = new int[] { 381, 382, 383, 384 };
        int[] femaleRedguardTextures = new int[] { 395, 396, 397, 398 };

        int[] maleNordTextures = new int[] { 387, 388, 389, 390 };
        int[] femaleNordTextures = new int[] { 392, 393, 451, 452 };

        int[] maleBretonTextures = new int[] { 385, 386, 391, 394 };
        int[] femaleBretonTextures = new int[] { 453, 454, 455, 456 };

        int[] guardTextures = { 399 };

        #endregion

        #region Animations

        const int IdleAnimSpeed = 1;
        const int MoveAnimSpeed = 4;

        static MobileAnimation[] IdleAnims = new MobileAnimation[]
        {
            new MobileAnimation() {Record = 5, FramePerSecond = IdleAnimSpeed, FlipLeftRight = false},          // Idle
        };

        static MobileAnimation[] IdleAnimsGuard = new MobileAnimation[]
        {
            new MobileAnimation() {Record = 15, FramePerSecond = IdleAnimSpeed, FlipLeftRight = false},          // Guard idle
        };

        static MobileAnimation[] MoveAnims = new MobileAnimation[]
        {
            new MobileAnimation() {Record = 0, FramePerSecond = MoveAnimSpeed, FlipLeftRight = false},          // Facing south (front facing player)
            new MobileAnimation() {Record = 1, FramePerSecond = MoveAnimSpeed, FlipLeftRight = false},          // Facing south-west
            new MobileAnimation() {Record = 2, FramePerSecond = MoveAnimSpeed, FlipLeftRight = false},          // Facing west
            new MobileAnimation() {Record = 3, FramePerSecond = MoveAnimSpeed, FlipLeftRight = false},          // Facing north-west
            new MobileAnimation() {Record = 4, FramePerSecond = MoveAnimSpeed, FlipLeftRight = false},          // Facing north (back facing player)
            new MobileAnimation() {Record = 3, FramePerSecond = MoveAnimSpeed, FlipLeftRight = true},           // Facing north-east
            new MobileAnimation() {Record = 2, FramePerSecond = MoveAnimSpeed, FlipLeftRight = true},           // Facing east
            new MobileAnimation() {Record = 1, FramePerSecond = MoveAnimSpeed, FlipLeftRight = true},           // Facing south-east
        };

        enum AnimStates
        {
            Idle,           // Idle facing player
            Move,           // Moving
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets idle state.
        /// Daggerfall NPCs are either in or motion or idle facing player.
        /// This only controls anim state, actual motion is handled by MobilePersonMotor.
        /// </summary>
        public sealed override bool IsIdle
        {
            get { return (currentAnimState == AnimStates.Idle); }
            set { SetIdle(value); }
        }

        #endregion

        #region Unity

        private void Start()
        {
            if (Application.isPlaying)
            {
                // Get component references
                mainCamera = GameManager.Instance.MainCamera;
                meshFilter = GetComponent<MeshFilter>();
            }

            // Mobile NPC shadows if enabled
            if (DaggerfallUnity.Settings.MobileNPCShadows)
                GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        }

        private void Update()
        {
            // Rotate to face camera in game
            if (mainCamera && Application.isPlaying)
            {
                // Rotate billboard based on camera facing
                cameraPosition = mainCamera.transform.position;
                Vector3 viewDirection = -new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z);
                transform.LookAt(transform.position + viewDirection);

                // Orient based on camera position
                UpdateOrientation();

                // Tick animations
                animTimer += Time.deltaTime;
                if (animTimer > 1f / animSpeed)
                {
                    if (++currentFrame >= stateAnims[0].NumFrames)
                        currentFrame = 0;

                    UpdatePerson(lastOrientation);
                    animTimer = 0;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Setup this person based on race and gender.
        /// </summary>
        public override void SetPerson(Races race, Genders gender, int personVariant, bool isGuard, int personFaceVariant, int personFaceRecordId)
        {
            SetPerson(race, gender, personVariant, isGuard, personFaceVariant, personFaceRecordId, Random.Range(0, VillagerVariety.NUM_VARIANTS));
        }
        public void SetPerson(Races race, Genders gender, int personVariant, bool isGuard, int personFaceVariant, int personFaceRecordId, int personVariety)
        {
            // Must specify a race
            if (race == Races.None)
                return;

            // Get texture range for this race and gender
            int[] textures = null;

            isUsingGuardTexture = isGuard;

            if (isGuard)
            {
                textures = guardTextures;
            }
            else
            {
                switch (race)
                {
                    case Races.Redguard:
                        textures = (gender == Genders.Male) ? maleRedguardTextures : femaleRedguardTextures;
                        break;
                    case Races.Nord:
                        textures = (gender == Genders.Male) ? maleNordTextures : femaleNordTextures;
                        break;
                    case Races.Breton:
                    default:
                        textures = (gender == Genders.Male) ? maleBretonTextures : femaleBretonTextures;
                        break;
                }
            }

            // Setup person rendering, selecting random variant and setting current season
            int archive = textures[personVariant];
            string season = VillagerVariety.seasonStrs[(int)DaggerfallUnity.Instance.WorldTime.Now.SeasonValue];

// Carademono: Testing lines to make all mobile NPCs a specific sprite - uncomment as required while testing (note: talk face wont match)
            if (!isGuard)
            {
                //archive = 396;
                //personFaceRecordId = 158;
                //personVariety = 1;
                //season = "";
            }

            Debug.LogFormat("Setting up villager variant: {0:000}.{1}.{2}{3}", archive, personFaceRecordId, personVariety, season);

            CacheRecordSizesAndFrames(archive);

            AssignMeshAndMaterial(archive, personFaceRecordId, personVariety, season);

            // Setup animation state
            moveAnims = GetStateAnims(AnimStates.Move);
            idleAnims = GetStateAnims(AnimStates.Idle);
            stateAnims = moveAnims;
            animSpeed = stateAnims[0].FramePerSecond;
            currentAnimState = AnimStates.Move;
            lastOrientation = -1;
            UpdateOrientation();
        }

        /// <summary>
        /// Gets billboard size.
        /// </summary>
        /// <returns>Vector2 of billboard width and height.</returns>
        public sealed override Vector3 GetSize()
        {
            if (recordSizes == null || recordSizes.Length == 0)
                return Vector2.zero;

            return recordSizes[0];
        }

        #endregion

        #region Private Methods

        private Material LoadVillagerVariant(int archive, int faceRecord, int variant, string season, MeshFilter meshFilter, ref MobileBillboardImportedTextures importedTextures)
        {
            if (isUsingGuardTexture)
                return null;

            Mod mod = VillagerVariety.mod;

            // Make material
            Material material = MaterialReader.CreateStandardMaterial(MaterialReader.CustomBlendMode.Cutout);

            // Check the cache for previously loaded variant textures
            string firstFrameName = VillagerVariety.GetImageName(archive, 0, 0, faceRecord, variant, season);
            if (textureCache.ContainsKey(firstFrameName))
            {
                importedTextures.Albedo = textureCache[firstFrameName];
                if (emmisionCache.ContainsKey(firstFrameName))
                {
                    importedTextures.IsEmissive = true;
                    material.EnableKeyword(EMISSION);
                    material.SetColor(EMISSIONCOLOR, Color.white);
                    importedTextures.EmissionMaps = emmisionCache[firstFrameName];
                }
            }
            else
            {
                // Load texture file to get record and frame count
                string fileName = TextureFile.IndexToFileName(archive);
                var textureFile = new TextureFile(Path.Combine(DaggerfallUnity.Instance.Arena2Path, fileName), FileUsage.UseMemory, true);

                // Check this season & variant is availible, use no season, then variant 0 if not
                if (!mod.HasAsset(firstFrameName))
                {
                    if (!string.IsNullOrEmpty(season))
                    {
                        season = string.Empty;
                        if (!mod.HasAsset(VillagerVariety.GetImageName(archive, 0, 0, faceRecord, variant, season)))
                            variant = 0;
                    }
                    else
                        variant = 0;
                }
                if (!mod.HasAsset(VillagerVariety.GetImageName(archive, 0, 0, faceRecord, variant, season)))
                {
                    Debug.LogFormat("No villager variant found after fallback: {0:000}.{1}.{2}{3}", archive, faceRecord, variant, season);
                    return null;
                }

                // Check whether there are emission textures (must exist for first frame)
                if (importedTextures.IsEmissive = mod.HasAsset(VillagerVariety.GetImageName(archive, 0, 0, faceRecord, variant, season) + EMISSION))
                {
                    material.EnableKeyword(EMISSION);
                    material.SetColor(EMISSIONCOLOR, Color.white);
                }

                // Import all textures in this archive
                importedTextures.Albedo = new Texture2D[textureFile.RecordCount][];
                importedTextures.EmissionMaps = importedTextures.IsEmissive ? new Texture2D[textureFile.RecordCount][] : null;

                for (int record = 0; record < textureFile.RecordCount; record++)
                {
                    int frames = textureFile.GetFrameCount(record);
                    var frameTextures = new Texture2D[frames];
                    var frameEmissionMaps = importedTextures.IsEmissive ? new Texture2D[frames] : null;

                    for (int frame = 0; frame < frames; frame++)
                    {
                        string faceFileName = VillagerVariety.GetImageName(archive, record, frame, faceRecord, variant, season);
                        string nofaceFileName = VillagerVariety.GetImageName(archive, record, frame, variant, season);
                        if (mod.HasAsset(faceFileName))
                            frameTextures[frame] = mod.GetAsset<Texture2D>(faceFileName);
                        else if (mod.HasAsset(nofaceFileName))
                            frameTextures[frame] = mod.GetAsset<Texture2D>(nofaceFileName);                 // Fallback to no face replacement
                        else
                            frameTextures[frame] = ImageReader.GetTexture(fileName, record, frame, true);   // Use vanilla texture/override if no custom frame

                        if (frameEmissionMaps != null)
                        {
                            Texture2D emissTex = mod.GetAsset<Texture2D>(VillagerVariety.GetImageName(archive, record, frame, faceRecord, variant, season) + EMISSION);
                            frameEmissionMaps[frame] = emissTex ?? frameTextures[frame];
                        }
                    }

                    importedTextures.Albedo[record] = frameTextures;
                    if (importedTextures.EmissionMaps != null)
                        importedTextures.EmissionMaps[record] = frameEmissionMaps;
                }
                Debug.LogFormat("Loaded villager variant: {0:000}.{1}.{2}{3}", archive, faceRecord, variant, season);

                // Add loaded textures to the cache
                textureCache[firstFrameName] = importedTextures.Albedo;
                if (importedTextures.EmissionMaps != null)
                    emmisionCache[firstFrameName] = importedTextures.EmissionMaps;
            }

            // Update UV map and indicate imported textures should be used
            SetUv(meshFilter);
            importedTextures.HasImportedTextures = true;

            return material;
        }

        /// <summary>
        /// Set UV Map for a planar mesh.
        /// </summary>
        /// <param name="x">Offset on X axis.</param>
        /// <param name="y">Offset on Y axis.</param>
        private static void SetUv(MeshFilter meshFilter, float x = 0, float y = 0)
        {
            Vector2[] uv = new Vector2[4];
            uv[0] = new Vector2(x, 1 - y);
            uv[1] = new Vector2(1 - x, 1 - y);
            uv[2] = new Vector2(x, y);
            uv[3] = new Vector2(1 - x, y);
            meshFilter.mesh.uv = uv;
        }


        void SetIdle(bool idle)
        {
            if (idleAnims == null || moveAnims == null)
                return;     // Protect against update() sequencing exception

            if (idle)
            {
                // Switch animation state to idle
                currentAnimState = AnimStates.Idle;
                stateAnims = idleAnims;
                currentFrame = 0;
                lastOrientation = -1;
                animTimer = 1;
                animSpeed = stateAnims[0].FramePerSecond;
            }
            else
            {
                // Switch animation state to move
                currentAnimState = AnimStates.Move;
                stateAnims = moveAnims;
                currentFrame = 0;
                lastOrientation = -1;
                animTimer = 1;
                animSpeed = stateAnims[0].FramePerSecond;
            }
        }

        private void CacheRecordSizesAndFrames(int textureArchive)
        {
            // Open texture file
            string path = Path.Combine(DaggerfallUnity.Instance.Arena2Path, TextureFile.IndexToFileName(textureArchive));
            TextureFile textureFile = new TextureFile(path, FileUsage.UseMemory, true);

            // Cache size and scale for each record
            recordSizes = new Vector2[textureFile.RecordCount];
            recordFrames = new int[textureFile.RecordCount];
            for (int i = 0; i < textureFile.RecordCount; i++)
            {
                // Get size and scale of this texture
                DFSize size = textureFile.GetSize(i);
                DFSize scale = textureFile.GetScale(i);

                // Set start size
                Vector2 startSize;
                startSize.x = size.Width;
                startSize.y = size.Height;

                // Apply scale
                Vector2 finalSize;
                int xChange = (int)(size.Width * (scale.Width / BlocksFile.ScaleDivisor));
                int yChange = (int)(size.Height * (scale.Height / BlocksFile.ScaleDivisor));
                finalSize.x = (size.Width + xChange);
                finalSize.y = (size.Height + yChange);

                // Set optional scale
                TextureReplacement.SetBillboardScale(textureArchive, i, ref finalSize);

                // Store final size and frame count
                recordSizes[i] = finalSize * MeshReader.GlobalScale;
                recordFrames[i] = textureFile.GetFrameCount(i);
            }
        }

        private void AssignMeshAndMaterial(int textureArchive, int faceRecord, int variant, string season)
        {
            // Get mesh filter
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            // Vertices for a 1x1 unit quad
            // This is scaled to correct size depending on facing and orientation
            float hx = 0.5f, hy = 0.5f;
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(hx, hy, 0);
            vertices[1] = new Vector3(-hx, hy, 0);
            vertices[2] = new Vector3(hx, -hy, 0);
            vertices[3] = new Vector3(-hx, -hy, 0);

            // Indices
            int[] indices = new int[6]
            {
                0, 1, 2,
                3, 2, 1,
            };

            // Normals
            Vector3 normal = Vector3.Normalize(Vector3.up + Vector3.forward);
            Vector3[] normals = new Vector3[4];
            normals[0] = normal;
            normals[1] = normal;
            normals[2] = normal;
            normals[3] = normal;

            // Create mesh
            Mesh mesh = new Mesh();
            mesh.name = string.Format("MobilePersonMesh");
            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.normals = normals;

            // Assign mesh
            meshFilter.sharedMesh = mesh;

            // Create material
            Material material =
                LoadVillagerVariant(textureArchive, faceRecord, variant, season, meshFilter, ref importedTextures) ??
                TextureReplacement.GetMobileBillboardMaterial(textureArchive, meshFilter, ref importedTextures) ??
                DaggerfallUnity.Instance.MaterialReader.GetMaterialAtlas(
                textureArchive,
                0,
                4,
                1024,
                out atlasRects,
                out atlasIndices,
                4,
                true,
                0,
                false,
                true);

            // Set new person material
            GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void UpdateOrientation()
        {
            Transform parent = transform.parent;
            if (parent == null)
                return;

            // Get direction normal to camera, ignore y axis
            Vector3 dir = Vector3.Normalize(
                new Vector3(cameraPosition.x, 0, cameraPosition.z) -
                new Vector3(transform.position.x, 0, transform.position.z));

            // Get parent forward normal, ignore y axis
            Vector3 parentForward = transform.parent.forward;
            parentForward.y = 0;

            // Get angle and cross product for left/right angle
            facingAngle = Vector3.Angle(dir, parentForward);
            facingAngle = facingAngle * -Mathf.Sign(Vector3.Cross(dir, parentForward).y);

            // Facing index
            int orientation = - Mathf.RoundToInt(facingAngle / anglePerOrientation);
            // Wrap values to 0 .. numberOrientations-1
            orientation = (orientation + numberOrientations) % numberOrientations;

            // Change person to this orientation
            if (orientation != lastOrientation)
                UpdatePerson(orientation);
        }

        private void UpdatePerson(int orientation)
        {
            if (stateAnims == null || stateAnims.Length == 0)
                return;

            // Get mesh filter
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            // Idle only has a single orientation
            if (currentAnimState == AnimStates.Idle && orientation != 0)
                orientation = 0;

            // Get person size and scale for this state
            int record = stateAnims[orientation].Record;
            Vector2 size = recordSizes[record];

            // Set mesh scale for this state
            transform.localScale = new Vector3(size.x, size.y, 1);

            // Check if orientation flip needed
            bool flip = stateAnims[orientation].FlipLeftRight;

            // Update Record/Frame texture
            if (importedTextures.HasImportedTextures)
            {
                if (meshRenderer == null)
                    meshRenderer = GetComponent<MeshRenderer>();

                // Assign imported texture
                meshRenderer.sharedMaterial.mainTexture = importedTextures.Albedo[record][currentFrame];
                if (importedTextures.IsEmissive)
                    meshRenderer.material.SetTexture(EMISSIONMAP, importedTextures.EmissionMaps[record][currentFrame]);

                // Update UVs on mesh
                Vector2[] uvs = new Vector2[4];
                if (flip)
                {
                    uvs[0] = new Vector2(1, 1);
                    uvs[1] = new Vector2(0, 1);
                    uvs[2] = new Vector2(1, 0);
                    uvs[3] = new Vector2(0, 0);
                }
                else
                {
                    uvs[0] = new Vector2(0, 1);
                    uvs[1] = new Vector2(1, 1);
                    uvs[2] = new Vector2(0, 0);
                    uvs[3] = new Vector2(1, 0);
                }
                meshFilter.sharedMesh.uv = uvs;
            }
            else
            {
                // Daggerfall Atlas: Update UVs on mesh
                Rect rect = atlasRects[atlasIndices[record].startIndex + currentFrame];
                Vector2[] uvs = new Vector2[4];
                if (flip)
                {
                    uvs[0] = new Vector2(rect.xMax, rect.yMax);
                    uvs[1] = new Vector2(rect.x, rect.yMax);
                    uvs[2] = new Vector2(rect.xMax, rect.y);
                    uvs[3] = new Vector2(rect.x, rect.y);
                }
                else
                {
                    uvs[0] = new Vector2(rect.x, rect.yMax);
                    uvs[1] = new Vector2(rect.xMax, rect.yMax);
                    uvs[2] = new Vector2(rect.x, rect.y);
                    uvs[3] = new Vector2(rect.xMax, rect.y);
                }
                meshFilter.sharedMesh.uv = uvs;
            }

            // Assign new orientation
            lastOrientation = orientation;
        }

        private MobileAnimation[] GetStateAnims(AnimStates state)
        {
            // Clone static animation state
            MobileAnimation[] anims;
            switch (state)
            {
                case AnimStates.Move:
                    anims = (MobileAnimation[])MoveAnims.Clone();
                    break;
                case AnimStates.Idle:
                    if (isUsingGuardTexture)
                        anims = (MobileAnimation[])IdleAnimsGuard.Clone();
                    else
                        anims = (MobileAnimation[])IdleAnims.Clone();
                    break;
                default:
                    return null;
            }

            // Assign number number of frames per anim
            for (int i = 0; i < anims.Length; i++)
                anims[i].NumFrames = recordFrames[anims[i].Record];

            return anims;
        }

        #endregion
    }
}
