﻿using System;
using System.Linq;
using JetBrains.Annotations;
using VelocityDb.Session;

namespace SoundExplorersDatabase.Data {
  public static class QueryHelper {
    [CanBeNull]
    public static TPersistable Find<TPersistable>(
      [CanBeNull] string key,
      [NotNull] SessionBase session) where TPersistable : RelativeBase {
      return Find<TPersistable>(
        persistable => persistable.Key == key, session);
    }

    [CanBeNull]
    public static TPersistable Find<TPersistable>(
      [NotNull] Func<TPersistable, bool> predicate,
      [NotNull] SessionBase session) where TPersistable : RelativeBase {
      if (!Schema.Instance.ExistsOnDatabase(session)) {
        return null;
      }
      return session.AllObjects<TPersistable>()
        .FirstOrDefault(predicate);
    }

    [NotNull]
    public static TPersistable Read<TPersistable>(
      [CanBeNull] string key,
      [NotNull] SessionBase session) where TPersistable : RelativeBase {
      return session.AllObjects<TPersistable>()
        .First(persistable => persistable.Key == key);
    }
  }
}