using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using VelocityDb;
using VelocityDb.Session;
using VelocityDb.TypeInfo;

namespace SoundExplorersDatabase {
  /// <summary>
  ///   Base class for entity types that have one-to-many
  ///   and/or many-to-one relations with other entity types.
  /// </summary>
  public abstract class EntityBase : ReferenceTracked, IEntity {
    private IDictionary<Type, IDictionary> _childrenOfType;
    private IDictionary<Type, IRelationInfo> _childrenRelations;
    private EntityBase _identifyingParent;
    private IDictionary<Type, IRelationInfo> _parentRelations;
    private IDictionary<Type, EntityBase> _parents;
    private QueryHelper _queryHelper;
    private Schema _schema;
    [CanBeNull] private string _simpleKey;

    /// <summary>
    ///   Creates an instance of an entity.
    /// </summary>
    /// <param name="entityType">
    ///   The main Type as which the entity will be persisted on the database.
    /// </param>
    /// <param name="simpleKeyName">
    ///   The name, for use in error messages, of the perstistable
    ///   public property corresponding to the simple key.
    /// </param>
    /// <param name="identifyingParentType">
    ///   Where applicable, the entity type of the identifying parent entity.
    /// </param>
    protected EntityBase([NotNull] Type entityType,
      [NotNull] string simpleKeyName, [CanBeNull] Type identifyingParentType) {
      EntityType = entityType ??
                   throw new ArgumentNullException(
                     nameof(entityType));
      SimpleKeyName = simpleKeyName ??
                      throw new ArgumentNullException(nameof(simpleKeyName));
      IdentifyingParentType = identifyingParentType;
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

    /// <summary>
    ///   The main Type as which the entity will be persisted on the database.
    ///   Entities of subtypes may be persisted but will be members of the same
    ///   child collections, if any, as entities of the main Type.
    /// </summary>
    [NotNull]
    internal Type EntityType { get; }

    [CanBeNull] private Type IdentifyingParentType { get; }
    private bool IsAddingToOrRemovingFromIdentifyingParent { get; set; }
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
    private IDictionary<Type, EntityBase> Parents {
      get {
        InitialiseIfNull(_parents);
        return _parents;
      }
      set {
        UpdateNonIndexField();
        _parents = value;
      }
    }

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

    /// <summary>
    ///   The identifying parent entity, which, where applicable,
    ///   uniquely identifies this entity in combination with SimpleKey.
    /// </summary>
    /// <remarks>
    ///   Not applicable to top-level entity types,
    ///   i.e. those with no many-to-one  relations to other entity types.
    ///   Derived classes should set this to the value of the corresponding
    ///   perstistable public property.
    ///   This is null initially and must be set before persistence
    ///   to a parent entity, of type specified by IdentifyingParentType,
    ///   that already persists.
    /// </remarks>
    [CanBeNull]
    public EntityBase IdentifyingParent {
      get => _identifyingParent;
      protected set {
        if (IsAddingToOrRemovingFromIdentifyingParent) {
          _identifyingParent = value;
          return;
        }
        if (IdentifyingParentType == null) {
          throw new ConstraintException(
            "An identifying parent type has not been specified " +
            $"for {EntityType.Name} '{Key}'.");
        }
        if (value == null) {
          throw new NoNullAllowedException(
            "A null reference has been specified as the " +
            $"{IdentifyingParentType.Name} for {EntityType.Name} '{Key}'.");
        }
        if (value.EntityType != IdentifyingParentType) {
          throw new ConstraintException(
            $"A {value.EntityType.Name} has been specified as the " +
            $"IdentifyingParent for {EntityType.Name} '{Key}'. " +
            $"A {IdentifyingParentType.Name} is expected'.");
        }
        var newKey = new Key(SimpleKey, value);
        value.CheckForDuplicateChild(EntityType, newKey);
        if (_identifyingParent != null &&
            // Should always be true
            _identifyingParent.ChildrenOfType[EntityType].Contains(Key)) {
          _identifyingParent.ChildrenOfType[EntityType].Remove(Key);
          _identifyingParent.References.Remove(
            _identifyingParent.References.First(r => r.To.Equals(this)));
        }
        value.ChildrenOfType[EntityType].Add(newKey, this);
        value.References.AddFast(new Reference(this, "_children"));
        Parents[IdentifyingParentType] = value;
        _identifyingParent = value;
      }
    }

    /// <summary>
    ///   Derived from SimpleKey and, where applicable, IdentifyingParent,
    ///   this is used as the key of this entity in any
    ///   SortedChildLists of which it is a member.
    /// </summary>
    [NotNull]
    public Key Key { get; }

    /// <summary>
    ///   In combination with the optional IdentifyingParent,
    ///   uniquely identifies the entity.
    /// </summary>
    /// <remarks>
    ///   Derived classes should set this to the value,
    ///   converted to string if necessary, of the corresponding
    ///   perstistable public property.
    ///   It is null only initially.  It must be set before persistence.
    /// </remarks>
    [CanBeNull]
    public string SimpleKey {
      get => _simpleKey;
      protected set {
        CheckCanChangeSimpleKey(_simpleKey, value);
        _simpleKey = value;
      }
    }

    internal void AddChild([NotNull] EntityBase child) {
      CheckCanAddChild(child);
      ChildrenOfType[child.EntityType].Add(CreateChildKey(child), child);
      References.AddFast(new Reference(child, "_children"));
      UpdateChild(child, this);
    }

    protected void ChangeNonIdentifyingParent(
      [NotNull] Type parentEntityType,
      [CanBeNull] EntityBase newParent) {
      Parents[parentEntityType]?.RemoveChild(this, newParent != null);
      newParent?.AddChild(this);
      Parents[parentEntityType] = newParent;
    }

    private void CheckCanAddChild([NotNull] EntityBase child) {
      if (child == null) {
        throw new NoNullAllowedException(
          "A null reference has been specified. " +
          $"So addition to {EntityType.Name} '{Key}' is not supported.");
      }
      if (child.Parents[EntityType] != null) {
        throw new ConstraintException(
          $"{child.EntityType.Name} '{child.Key}' " +
          $"cannot be added to {EntityType.Name} '{Key}', " +
          $"because it already belongs to {EntityType.Name} " +
          $"'{child.Parents[EntityType].Key}'.");
      }
      CheckForDuplicateChild(child.EntityType, CreateChildKey(child));
    }

    private void CheckCanChangeSimpleKey(
      [CanBeNull] string oldSimpleKey, [CanBeNull] string newSimpleKey) {
      if (newSimpleKey == null) {
        throw new NoNullAllowedException(
          $"A null reference has been specified as the {SimpleKeyName} " +
          $"for {EntityType.Name} '{Key}'. " +
          $"Null {SimpleKeyName}s are not supported.");
      }
      if (newSimpleKey == oldSimpleKey) {
        return;
      }
      if (!IsTopLevel) {
        IdentifyingParent?.CheckForDuplicateChild(EntityType,
          new Key(newSimpleKey, IdentifyingParent));
        return;
      }
      if (!IsPersistent || Session == null) {
        return;
      }
      // If there's no session, which means we cannot check for a duplicate,
      // EntityBase.UpdateNonIndexField should already have thrown 
      // an InvalidOperationException.
      if (QueryHelper.FindDuplicateSimpleKey(EntityType, Oid, newSimpleKey,
        Session) != null) {
        throw new DuplicateKeyException(
          this,
          $"{EntityType.Name}'s {SimpleKeyName} cannot be changed to " +
          $"{newSimpleKey} because another {EntityType.Name} " +
          $"with the that {SimpleKeyName} has already been persisted.");
      }
    }

    protected virtual void CheckCanPersist([NotNull] SessionBase session) {
      if (SimpleKey == null) {
        throw new NoNullAllowedException(
          $"A {SimpleKeyName} has not yet been specified. " +
          $"So the {EntityType.Name} cannot be persisted.");
      }
      foreach (var parentKeyValuePair in Parents) {
        var parentType = parentKeyValuePair.Key;
        var parent = parentKeyValuePair.Value;
        if (parent == null && ParentRelations[parentType].IsMandatory) {
          throw new ConstraintException(
            $"{EntityType.Name} '{Key}' " +
            $"cannot be persisted because its {parentType.Name} "
            + "has not been specified.");
        }
      }
      if (IsTopLevel &&
          QueryHelper.FindDuplicateSimpleKey(EntityType, Oid, SimpleKey,
            session) != null) {
        throw new DuplicateKeyException(
          this,
          $"{EntityType.Name} '{Key}' " +
          $"cannot be persisted because another {EntityType.Name} "
          + "with the same key already persists.");
      }
    }

    private void CheckCanRemoveChild(
      [NotNull] EntityBase child, bool isReplacingOrUnpersisting) {
      if (child == null) {
        throw new NoNullAllowedException(
          "A null reference has been specified. " +
          $"So removal from {EntityType.Name} '{Key}' is not supported.");
      }
      if (!ChildrenOfType[child.EntityType].Contains(child.Key)) {
        throw new KeyNotFoundException(
          $"{child.EntityType.Name} '{child.Key}' " +
          $"cannot be removed from {EntityType.Name} '{Key}', " +
          $"because it does not belong to {EntityType.Name} " +
          $"'{Key}'.");
      }
      if (ChildrenRelations[child.EntityType].IsMandatory &&
          !isReplacingOrUnpersisting) {
        throw new ConstraintException(
          $"{child.EntityType.Name} '{child.Key}' " +
          $"cannot be removed from {EntityType.Name} '{Key}', " +
          "because membership is mandatory.");
      }
    }

    private void CheckForDuplicateChild([NotNull] Type childEntityType,
      [NotNull] Key keyToCheck) {
      if (ChildrenOfType[childEntityType]
        .Contains(keyToCheck)) {
        throw new DuplicateKeyException(
          keyToCheck,
          $"{childEntityType.Name} '{keyToCheck}' " +
          $"cannot be added to {EntityType.Name} '{Key}', " +
          $"because a {childEntityType.Name} with that Key " +
          $"already belongs to the {EntityType.Name}.");
      }
    }

    [NotNull]
    private Key CreateChildKey([NotNull] EntityBase child) {
      return new Key(child,
        child.IdentifyingParentType == EntityType ? this : null);
    }

    [NotNull]
    private IDictionary<Type, IRelationInfo> CreateChildrenRelations() {
      var values =
        from relation in Schema.Relations
        where relation.ParentType == EntityType
        select relation;
      return values.ToDictionary<RelationInfo, Type, IRelationInfo>(
        value => value.ChildType, value => value);
    }

    [NotNull]
    private IDictionary<Type, IRelationInfo> CreateParentRelations() {
      var values =
        from relation in Schema.Relations
        where relation.ChildType == EntityType
        select relation;
      return values.ToDictionary<RelationInfo, Type, IRelationInfo>(
        value => value.ParentType, value => value);
    }

    [NotNull]
    private string CreateReferentialIntegrityViolationMessage() {
      var list = new SortedList<string, int>();
      foreach (var childOfTypeKvp in ChildrenOfType) {
        if (childOfTypeKvp.Value.Count > 0) {
          list.Add(childOfTypeKvp.Key.Name, childOfTypeKvp.Value.Count);
        }
      }
      var writer = new StringWriter();
      writer.Write(
        $"{EntityType.Name} '{Key}' cannot be deleted because it is referenced by ");
      for (var i = 0; i < list.Count; i++) {
        writer.Write($"{list.Values[i]:N0} {list.Keys[i]}s");
        writer.Write(i < list.Count - 1 ? ", " : ".");
      }
      return writer.ToString();
    }

    /// <summary>
    ///   Allows a derived entity to return
    ///   its SortedChildList of child entities of the specified entity type.
    /// </summary>
    [NotNull]
    protected abstract IDictionary GetChildren([NotNull] Type childType);

    private void Initialise() {
      ParentRelations = CreateParentRelations();
      Parents = new Dictionary<Type, EntityBase>();
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

    public override ulong Persist(Placement place, SessionBase session,
      bool persistRefs = true,
      bool disableFlush = false,
      Queue<IOptimizedPersistable> toPersist = null) {
      CheckCanPersist(session);
      return base.Persist(place, session, persistRefs, disableFlush, toPersist);
    }

    internal void RemoveChild([NotNull] EntityBase child,
      bool isReplacingOrUnpersisting) {
      CheckCanRemoveChild(child, isReplacingOrUnpersisting);
      ChildrenOfType[child.EntityType].Remove(child.Key);
      References.Remove(References.First(r => r.To.Equals(child)));
      UpdateChild(child, null);
    }

    public override void Unpersist(SessionBase session) {
      if (References.Count > 0) {
        // If we did not do this,
        // VelocityDB would throw a ReferentialIntegrityException
        // on base.Unpersist.
        throw new ConstraintException(
          CreateReferentialIntegrityViolationMessage());
      }
      RemoveFromAllParents();
      base.Unpersist(session);

      void RemoveFromAllParents() {
        var parents =
          Parents.Values.Where(parent => parent != null).ToList();
        for (int i = parents.Count - 1; i >= 0; i--) {
          parents[i].RemoveChild(this, true);
        }
      }
    }

    private void UpdateChild([NotNull] EntityBase child,
      [CanBeNull] EntityBase newParent) {
      child.UpdateNonIndexField();
      child.Parents[EntityType] = newParent;
      if (EntityType == child.IdentifyingParentType) {
        child.IsAddingToOrRemovingFromIdentifyingParent = true;
        child.IdentifyingParent = newParent;
        child.IsAddingToOrRemovingFromIdentifyingParent = false;
      } else {
        child.SetNonIdentifyingParentField(EntityType,
          newParent);
      }
    }

    /// <summary>
    ///   Allows a derived entity to update the field (not property)
    ///   corresponding to the parent entity of the specified entity type
    ///   with the specified new value.
    /// </summary>
    protected abstract void SetNonIdentifyingParentField(
      [NotNull] Type parentEntityType, [CanBeNull] EntityBase newParent);

    /// <summary>
    ///   Marks the entity as being updated, so that the entity will be written
    ///   at commit transaction. In this application, this is used instead of
    ///   OptimizedPersistable.Update, even for key properties,
    ///   because we don't use VelocityDbList for collections and indexing.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   If an update of a persisted entity is attempted outside a session,
    ///   provides an meaningful error message.
    /// </exception>
    protected new void UpdateNonIndexField() {
      try {
        base.UpdateNonIndexField();
      } catch (NullReferenceException ex) {
        throw new InvalidOperationException(
          $"{EntityType.Name} '{SimpleKey}' cannot be updated outside a session " +
          "because it has already been persisted.", ex);
      }
    }
  }
}