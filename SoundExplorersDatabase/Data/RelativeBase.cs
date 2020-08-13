using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Linq;
using JetBrains.Annotations;
using VelocityDb;
using VelocityDb.Session;
using VelocityDb.TypeInfo;

namespace SoundExplorersDatabase.Data {
  public abstract class RelativeBase : ReferenceTracked, IRelative {
    private IDictionary<Type, IDictionary> _childrenOfType;
    private IDictionary<Type, IRelationInfo> _childrenRelations;
    private IDictionary<Type, IRelationInfo> _parentRelations;
    private IDictionary<Type, RelativeBase> _parents;
    private QueryHelper _queryHelper;
    private Schema _schema;

    protected RelativeBase([NotNull] Type persistableType,
      [NotNull] string simpleKeyName) {
      PersistableType = persistableType ??
                        throw new ArgumentNullException(
                          nameof(persistableType));
      SimpleKeyName = simpleKeyName ??
                      throw new ArgumentNullException(nameof(simpleKeyName));
      Key = new Key(this);
    }

    [NotNull]
    private IDictionary<Type, IDictionary> ChildrenOfType {
      get {
        InitialiseIfNull(_childrenOfType);
        return _childrenOfType;
      }
      set {
        UpdateNonIndexField();
        _childrenOfType = value;
      }
    }

    [NotNull]
    private IDictionary<Type, IRelationInfo> ChildrenRelations {
      get {
        InitialiseIfNull(_childrenRelations);
        return _childrenRelations;
      }
      set {
        UpdateNonIndexField();
        _childrenRelations = value;
      }
    }

    private bool IsTopLevel => Parents.Count == 0;

    [NotNull]
    private IDictionary<Type, IRelationInfo> ParentRelations {
      get {
        InitialiseIfNull(_parentRelations);
        return _parentRelations;
      }
      set {
        UpdateNonIndexField();
        _parentRelations = value;
      }
    }

    [NotNull]
    private IDictionary<Type, RelativeBase> Parents {
      get {
        InitialiseIfNull(_parents);
        return _parents;
      }
      set {
        UpdateNonIndexField();
        _parents = value;
      }
    }

    [NotNull] private Type PersistableType { get; }

    [NotNull]
    internal QueryHelper QueryHelper {
      get => _queryHelper ?? (_queryHelper = QueryHelper.Instance);
      set => _queryHelper = value;
    }

    [NotNull]
    protected Schema Schema {
      get => _schema ?? (_schema = Schema.Instance);
      set => _schema = value;
    }

    [NotNull] private string SimpleKeyName { get; }

    [CanBeNull] public IRelative IdentifyingParent => GetIdentifyingParent();

    [NotNull] public Key Key { get; }

    [CanBeNull] public string SimpleKey => GetSimpleKey();

    internal void AddChild([NotNull] RelativeBase child) {
      CheckCanAddChild(child);
      ChildrenOfType[child.PersistableType].Add(child.Key, child);
      References.AddFast(new Reference(child, "_children"));
      UpdateChild(child, this);
    }

    protected void ChangeParent(
      [NotNull] Type parentPersistableType,
      [CanBeNull] RelativeBase newParent) {
      Parents[parentPersistableType]?.RemoveChild(this, newParent != null);
      newParent?.AddChild(this);
      Parents[parentPersistableType] = newParent;
    }

    private void CheckCanAddChild([NotNull] RelativeBase child) {
      if (child == null) {
        throw new NoNullAllowedException(
          "A null reference has been specified. " +
          $"So addition to {PersistableType.Name} '{Key}' is not supported.");
      }
      if (child.Parents[PersistableType] != null) {
        throw new ConstraintException(
          $"{child.PersistableType.Name} '{child.Key}' " +
          $"cannot be added to {PersistableType.Name} '{Key}', " +
          $"because it already belongs to {PersistableType.Name} " +
          $"'{child.Parents[PersistableType].Key}'.");
      }
      if (ChildrenOfType[child.PersistableType].Contains(child.Key)) {
        throw new DuplicateKeyException(
          child,
          $"{child.PersistableType.Name} '{child.Key}' " +
          $"cannot be added to {PersistableType.Name} '{Key}', " +
          $"because a {child.PersistableType.Name} with that Key " +
          $"already belongs to the {PersistableType.Name}.");
      }
    }

    protected virtual void CheckCanPersist([NotNull] SessionBase session) {
      if (Key == null) {
        throw new NoNullAllowedException(
          "A Key has not yet been specified. " +
          $"So the {PersistableType.Name} cannot be persisted.");
      }
      foreach (var parentKeyValuePair in Parents) {
        var parentType = parentKeyValuePair.Key;
        var parent = parentKeyValuePair.Value;
        if (parent == null && ParentRelations[parentType].IsMandatory) {
          throw new ConstraintException(
            $"{PersistableType.Name} '{Key}' " +
            $"cannot be persisted because its {parentType.Name} "
            + "has not been specified.");
        }
      }
      if (IsTopLevel && IsDuplicateKey(session)) {
        throw new DuplicateKeyException(
          this,
          $"{PersistableType.Name} '{Key}' " +
          $"cannot be persisted because another {PersistableType.Name} "
          + "with the same key already persists.");
      }
    }

    private void CheckCanRemoveChild(
      [NotNull] RelativeBase child, bool isReplacingOrUnpersisting) {
      if (child == null) {
        throw new NoNullAllowedException(
          "A null reference has been specified. " +
          $"So removal from {PersistableType.Name} '{Key}' is not supported.");
      }
      if (!ChildrenOfType[child.PersistableType].Contains(child.Key)) {
        throw new KeyNotFoundException(
          $"{child.PersistableType.Name} '{child.Key}' " +
          $"cannot be removed from {PersistableType.Name} '{Key}', " +
          $"because it does not belong to {PersistableType.Name} " +
          $"'{Key}'.");
      }
      if (ChildrenRelations[child.PersistableType].IsMandatory &&
          !isReplacingOrUnpersisting) {
        throw new ConstraintException(
          $"{child.PersistableType.Name} '{child.Key}' " +
          $"cannot be removed from {PersistableType.Name} '{Key}', " +
          "because membership is mandatory.");
      }
    }

    [NotNull]
    private IDictionary<Type, IRelationInfo> CreateChildrenRelations() {
      var values =
        from relation in Schema.Relations
        where relation.ParentType == PersistableType
        select relation;
      return values.ToDictionary<RelationInfo, Type, IRelationInfo>(
        value => value.ChildType, value => value);
    }

    [NotNull]
    private IDictionary<Type, IRelationInfo> CreateParentRelations() {
      var values =
        from relation in Schema.Relations
        where relation.ChildType == PersistableType
        select relation;
      return values.ToDictionary<RelationInfo, Type, IRelationInfo>(
        value => value.ParentType, value => value);
    }

    [CanBeNull]
    protected abstract RelativeBase FindWithSameKey(
      [NotNull] SessionBase session);

    [NotNull]
    protected abstract IDictionary GetChildren([NotNull] Type childType);

    [CanBeNull]
    protected abstract RelativeBase GetIdentifyingParent();

    [CanBeNull]
    protected abstract string GetSimpleKey();

    private void Initialise() {
      ParentRelations = CreateParentRelations();
      Parents = new Dictionary<Type, RelativeBase>();
      foreach (var relationKvp in ParentRelations) {
        Parents.Add(relationKvp.Key, null);
      }
      ChildrenRelations = CreateChildrenRelations();
      ChildrenOfType = new Dictionary<Type, IDictionary>();
      foreach (var relationKvp in ChildrenRelations) {
        ChildrenOfType.Add(relationKvp.Key, GetChildren(relationKvp.Key));
      }
    }

    private void InitialiseIfNull([CanBeNull] object backingField) {
      if (backingField == null) {
        Initialise();
      }
    }

    private bool IsDuplicateKey([NotNull] SessionBase session) {
      var existing = FindWithSameKey(session);
      return existing != null && !existing.Oid.Equals(Oid);
    }

    protected abstract void OnParentFieldToBeUpdated(
      [NotNull] Type parentPersistableType, [CanBeNull] RelativeBase newParent);

    public override ulong Persist(Placement place, SessionBase session,
      bool persistRefs = true,
      bool disableFlush = false,
      Queue<IOptimizedPersistable> toPersist = null) {
      CheckCanPersist(session);
      return base.Persist(place, session, persistRefs, disableFlush, toPersist);
    }

    internal void RemoveChild([NotNull] RelativeBase child,
      bool isReplacingOrUnpersisting) {
      CheckCanRemoveChild(child, isReplacingOrUnpersisting);
      UpdateChild(child, null);
      ChildrenOfType[child.PersistableType].Remove(child.Key);
      References.Remove(References.First(r => r.To.Equals(child)));
    }

    // protected void SetKey(
    //   [NotNull] string simpleKey) {
    //   if (simpleKey == null) {
    //     throw new NoNullAllowedException(
    //       $"A null reference has been specified as the {SimpleKeyName} " +
    //       $"for {PersistableType.Name} '{Key}'. " +
    //       $"Null {SimpleKeyName}s are not supported.");
    //   }
    //   if (identifyingParent == null) {
    //     if (IdentifyingParentType != null) {
    //       throw new NoNullAllowedException(
    //         "A null reference has been specified as the " +
    //         $"{IdentifyingParentType.Name} for {PersistableType.Name} '{Key}'.");
    //     }
    //   } else if (IdentifyingParentType != null &&
    //              identifyingParent.PersistableType != IdentifyingParentType) {
    //     throw new ConstraintException(
    //       $"A {identifyingParent.PersistableType.Name} has been specified as the " +
    //       $"{IdentifyingParentType.Name} for {PersistableType.Name} '{Key}'.");
    //   }
    //   Key.SimpleKey = simpleKey;
    //   Key.IdentifyingParent = identifyingParent;
    // }

    public override void Unpersist(SessionBase session) {
      var parents =
        Parents.Values.Where(parent => parent != null).ToList();
      for (int i = parents.Count - 1; i >= 0; i--) {
        parents[i].ChildrenOfType[PersistableType].Remove(this);
        parents[i].RemoveChild(this, true);
      }
      base.Unpersist(session);
    }

    private void UpdateChild([NotNull] RelativeBase child,
      [CanBeNull] RelativeBase newParent) {
      child.UpdateNonIndexField();
      child.Parents[PersistableType] = newParent;
      child.OnParentFieldToBeUpdated(PersistableType, newParent);
    }
  }
}