using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace WhereAreYouGoing
{
    public class WhereAreYouGoingSettings : ISettings
    {
        [Menu("Draw Monster")]
        public ToggleNode DrawMonsters { get; set; } = new ToggleNode(true);
        public ToggleNode MultiThreading { get; set; } = new ToggleNode(false);
        public ToggleNode DrawOnWorld { get; set; } = new ToggleNode(true);
        public ToggleNode DrawOnMap { get; set; } = new ToggleNode(true);
        public ColorNode AttackPathColor { get; set; } = new ColorNode(Color.Red);
        public ToggleNode ShowWhiteMonsterPath { get; set; } = new ToggleNode(false);
        public ColorNode WhiteMonsterPathColor { get; set; } = new ColorNode(Color.White);
        public ToggleNode ShowMagicMonsterPath { get; set; } = new ToggleNode(false);
        public ColorNode MagicMonsterPathColor { get; set; } = new ColorNode(Color.SkyBlue);
        public ToggleNode ShowRareMonsterPath { get; set; } = new ToggleNode(false);
        public ColorNode RareMonsterPathColor { get; set; } = new ColorNode(Color.Yellow);
        public ToggleNode ShowUniqueMonsterPath { get; set; } = new ToggleNode(false);
        public ColorNode UniqueMonsterPathColor { get; set; } = new ColorNode(Color.Gold);
        public ToggleNode Enable { get; set; } = new ToggleNode(true);
    }
}
