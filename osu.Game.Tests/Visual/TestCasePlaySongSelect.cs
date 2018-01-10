// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.MathUtils;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Carousel;
using osu.Game.Screens.Select.Filter;
using osu.Game.Tests.Platform;

namespace osu.Game.Tests.Visual
{
    public class TestCasePlaySongSelect : OsuTestCase
    {
        private BeatmapManager manager;

        private RulesetStore rulesets;

        private DependencyContainer dependencies;
        private WorkingBeatmap defaultBeatmap;

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(SongSelect),
            typeof(BeatmapCarousel),

            typeof(CarouselItem),
            typeof(CarouselGroup),
            typeof(CarouselGroupEagerSelect),
            typeof(CarouselBeatmap),
            typeof(CarouselBeatmapSet),

            typeof(DrawableCarouselItem),
            typeof(CarouselItemState),

            typeof(DrawableCarouselBeatmap),
            typeof(DrawableCarouselBeatmapSet),
        };

        protected override IReadOnlyDependencyContainer CreateLocalDependencies(IReadOnlyDependencyContainer parent) => dependencies = new DependencyContainer(parent);

        private class TestSongSelect : PlaySongSelect
        {
            public WorkingBeatmap CurrentBeatmap => Beatmap.Value;
            public WorkingBeatmap CurrentBeatmapDetailsBeatmap => BeatmapDetails.Beatmap;
            public new BeatmapCarousel Carousel => base.Carousel;
        }

        [BackgroundDependencyLoader]
        private void load(OsuGameBase game)
        {
            TestSongSelect songSelect = null;

            var storage = new TestStorage(@"TestCasePlaySongSelect");

            // this is by no means clean. should be replacing inside of OsuGameBase somehow.
            var context = new OsuDbContext();

            Func<OsuDbContext> contextFactory = () => context;

            dependencies.Cache(rulesets = new RulesetStore(contextFactory));
            dependencies.Cache(manager = new BeatmapManager(storage, contextFactory, rulesets, null)
            {
                DefaultBeatmap = defaultBeatmap = game.Beatmap.Default
            });

            void loadNewSongSelect(bool deleteMaps = false) => AddStep("reload song select", () =>
            {
                if (deleteMaps)
                {
                    manager.DeleteAll();
                    game.Beatmap.SetDefault();
                }

                if (songSelect != null)
                {
                    Remove(songSelect);
                    songSelect.Dispose();
                }

                Add(songSelect = new TestSongSelect());
            });

            loadNewSongSelect(true);

            AddWaitStep(3);

            AddAssert("dummy selected", () => songSelect.CurrentBeatmap == defaultBeatmap);

            AddAssert("dummy shown on wedge", () => songSelect.CurrentBeatmapDetailsBeatmap == defaultBeatmap);

            AddStep("import test maps", () =>
            {
                for (int i = 0; i < 100; i += 10)
                    manager.Import(createTestBeatmapSet(i));
            });

            AddWaitStep(3);
            AddAssert("random map selected", () => songSelect.CurrentBeatmap != defaultBeatmap);

            loadNewSongSelect();
            AddWaitStep(3);
            AddAssert("random map selected", () => songSelect.CurrentBeatmap != defaultBeatmap);

            AddStep(@"Sort by Artist", delegate { songSelect.FilterControl.Sort = SortMode.Artist; });
            AddStep(@"Sort by Title", delegate { songSelect.FilterControl.Sort = SortMode.Title; });
            AddStep(@"Sort by Author", delegate { songSelect.FilterControl.Sort = SortMode.Author; });
            AddStep(@"Sort by Difficulty", delegate { songSelect.FilterControl.Sort = SortMode.Difficulty; });
        }

        private BeatmapSetInfo createTestBeatmapSet(int i)
        {
            return new BeatmapSetInfo
            {
                OnlineBeatmapSetID = 1234 + i,
                Hash = new MemoryStream(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())).ComputeMD5Hash(),
                Metadata = new BeatmapMetadata
                {
                    OnlineBeatmapSetID = 1234 + i,
                    // Create random metadata, then we can check if sorting works based on these
                    Artist = "MONACA " + RNG.Next(0, 9),
                    Title = "Black Song " + RNG.Next(0, 9),
                    AuthorString = "Some Guy " + RNG.Next(0, 9),
                },
                Beatmaps = new List<BeatmapInfo>(new[]
                {
                    new BeatmapInfo
                    {
                        OnlineBeatmapID = 1234 + i,
                        Ruleset = rulesets.AvailableRulesets.First(),
                        Path = "normal.osu",
                        Version = "Normal",
                        BaseDifficulty = new BeatmapDifficulty
                        {
                            OverallDifficulty = 3.5f,
                        }
                    },
                    new BeatmapInfo
                    {
                        OnlineBeatmapID = 1235 + i,
                        Ruleset = rulesets.AvailableRulesets.First(),
                        Path = "hard.osu",
                        Version = "Hard",
                        BaseDifficulty = new BeatmapDifficulty
                        {
                            OverallDifficulty = 5,
                        }
                    },
                    new BeatmapInfo
                    {
                        OnlineBeatmapID = 1236 + i,
                        Ruleset = rulesets.AvailableRulesets.First(),
                        Path = "insane.osu",
                        Version = "Insane",
                        BaseDifficulty = new BeatmapDifficulty
                        {
                            OverallDifficulty = 7,
                        }
                    },
                }),
            };
        }
    }
}
