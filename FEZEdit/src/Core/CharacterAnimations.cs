using System.Collections.Generic;
using FEZRepacker.Core.Definitions.Game.Graphics;
using FEZRepacker.Core.Definitions.Game.NpcMetadata;

namespace FEZEdit.Core;

public record CharacterAnimations(IDictionary<string, AnimatedTexture> Animations, NpcMetadata Metadata);