namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// Downed clips on the Full Body Override layer (health.md §4). The editor rebuild imports and trims them from this
    /// table and builds the states. <see cref="Ragdoll"/> plays a fall for the body's gait, blends the end of it into
    /// a lying state, and rolls a face-up body onto its belly the first time it crawls.
    /// </summary>
    public static class DownedAnimation
    {
        /// <summary>Full Body Override in HumanCharacterAnimator. <c>AnimationOrchestrator</c> weights it 1 while Crawling.</summary>
        public const int Layer = 3;

        /// <summary>Belly down: a 1D blend on Speed from <see cref="Still"/> (0) to <see cref="Crawl"/> (walk speed).</summary>
        public const string CrawlState = "Crawl";

        /// <summary>On the back, lying still (Mix_Laying Breathless). Crawling rolls the body onto its belly first.</summary>
        public const string FaceUpState = "Downed Face Up";

        /// <summary>Animator Speed below this falls from standing; above <see cref="RunGaitSpeed"/> it falls from a run.</summary>
        public const float WalkGaitSpeed = 0.1f;
        public const float RunGaitSpeed = 0.65f;

        public readonly struct Clip
        {
            /// <summary>Full Body Override state name; empty for a clip used only inside the crawl blend.</summary>
            public readonly string StateName;
            /// <summary>Path relative to Assets/Art/Animations.</summary>
            public readonly string File;
            public readonly string ClipName;
            public readonly int FirstFrame;
            public readonly int LastFrame;
            public readonly bool Loop;

            public Clip(string stateName, string file, string clipName, int firstFrame, int lastFrame, bool loop)
            {
                StateName = stateName;
                File = file;
                ClipName = clipName;
                FirstFrame = firstFrame;
                LastFrame = lastFrame;
                Loop = loop;
            }
        }

        // Mixamo "Dying" from standing: steps back and folds onto its belly by frame ~99, then lies still.
        public static readonly Clip FallStanding = new(
            "Downed Fall Stand", "Downed/X Bot@Dying.fbx", "Mix_Dying", 0, 104, false);

        // Shooter Pack "walking to dying": falls forward onto its belly about 2 m on, still from frame ~65.
        public static readonly Clip FallWalking = new(
            "Downed Fall Walk", "Downed/X Bot@Walking To Dying.fbx", "Mix_WalkingToDying", 0, 70, false);

        // Shooter Pack "rifle run to dying": tumbles about 3.4 m and ends on its back, still from frame ~37.
        public static readonly Clip FallRunning = new(
            "Downed Fall Run", "Downed/X Bot@Rifle Run To Dying.fbx", "Mix_RifleRunToDying", 0, 44, false);

        /// <summary>The end of <see cref="FallStanding"/>, held: the belly-down pose while not crawling.</summary>
        public static readonly Clip Still = new(
            "", "Downed/X Bot@Dying.fbx", "Mix_DownedStill", 106, 132, true);

        // Shooter Pack "zombie crawl": flat on the belly, pulling with the arms, legs dragging. 0.38 m/s in place.
        public static readonly Clip Crawl = new(
            "", "Downed/X Bot@Zombie Crawl.fbx", "Mix_ZombieCrawl", 0, 154, true);

        public static readonly Clip[] Falls = { FallStanding, FallWalking, FallRunning };

        public static Clip FallFor(float animatorSpeed) =>
            animatorSpeed >= RunGaitSpeed ? FallRunning
            : animatorSpeed >= WalkGaitSpeed ? FallWalking
            : FallStanding;
    }
}
