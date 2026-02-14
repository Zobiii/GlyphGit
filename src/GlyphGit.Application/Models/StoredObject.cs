using GlyphGit.Domain.Objects;

namespace GlyphGit.Application.Models;

public sealed record StoredObject(GitObjectType Type, byte[] Payload);
