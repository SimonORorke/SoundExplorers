﻿using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace SoundExplorersDatabase.Data {
  public class Act : RelativeBase {
    private string _notes;

    public Act() : base(typeof(Act), nameof(Name), null) {
      Sets = new SortedChildList<Set>(this);
    }

    [CanBeNull]
    public string Name {
      get => SimpleKey;
      set {
        UpdateNonIndexField();
        SimpleKey = value;
      }
    }

    [CanBeNull]
    public string Notes {
      get => _notes;
      set {
        UpdateNonIndexField();
        _notes = value;
      }
    }

    [NotNull] public SortedChildList<Set> Sets { get; }

    protected override IDictionary GetChildren(Type childType) {
      return Sets;
    }

    [ExcludeFromCodeCoverage]
    protected override void OnNonIdentifyingParentFieldToBeUpdated(
      Type parentPersistableType, RelativeBase newParent) {
      throw new NotSupportedException();
    }
  }
}