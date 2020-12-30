﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using JetBrains.Annotations;
using SoundExplorers.Data;

namespace SoundExplorers.Model {
  /// <summary>
  ///   Represents an entity table in a form suitable (when converted to an array)
  ///   for populating a ComboBox for selecting a referenced entity.
  /// </summary>
  public class ReferenceableItemList : List<object> {
    //private static Newsletter _dummyNewsletter;

    public ReferenceableItemList([NotNull] BindingColumn referencingColumn) {
      ReferencingColumn = referencingColumn;
    }

    // private static Newsletter DummyNewsletter =>
    //   _dummyNewsletter ?? (_dummyNewsletter = new Newsletter());
    private IDictionary<string, IEntity> EntityDictionary { get; set; }
    [NotNull] private BindingColumn ReferencingColumn { get; }

    public bool ContainsKey([NotNull] string simpleKey) {
      return EntityDictionary.ContainsKey(simpleKey);
    }

    [NotNull]
    private static IDictionary<string, IEntity> CreateEntityDictionary(
      // ReSharper disable once SuggestBaseTypeForParameter
      [NotNull] IEntityList entities) {
      var result = new Dictionary<string, IEntity>();
      if (entities is NewsletterList) {
        result.Add(EntityBase.DateToSimpleKey(EntityBase.InitialDate), null);
      }
      foreach (IEntity entity in entities) {
        result.Add(ToSimpleKey(entity), entity);
      }
      return result;
    }

    [NotNull]
    private IEntityList CreateEntityList() {
      var result =
        Global.CreateEntityList(
          ReferencingColumn.ReferencedEntityListType ??
          throw new NullReferenceException(
            nameof(ReferencingColumn.ReferencedEntityListType)));
      result.Session = ReferencingColumn.Session;
      return result;
    }

    [NotNull]
    public static RowNotInTableException CreateReferencedEntityNotFoundException(
      [NotNull] string columnName, [NotNull] string simpleKey) {
      string message = $"{columnName} not found: '{Format(simpleKey)}'";
      // Debug.WriteLine("ReferenceableItemList.CreateReferencedEntityNotFoundException");
      // Debug.WriteLine($"    {message}");
      return new RowNotInTableException(message);
    }

    internal void Fetch() {
      var entities = CreateEntityList();
      entities.Populate(createBindingList: false);
      EntityDictionary = CreateEntityDictionary(entities);
      PopulateItems(entities);
    }

    /// <summary>
    ///   Returns the specified simple key formatted as it appears on the grid.
    /// </summary>
    [CanBeNull]
    private static string Format([CanBeNull] string simpleKey) {
      return DateTime.TryParse(simpleKey, out var date)
        ? date.ToString(Global.DateFormat)
        : simpleKey;
    }

    [CanBeNull]
    internal IEntity GetEntity([NotNull] object referencingPropertyValue) {
      string simpleKey = ToSimpleKey(referencingPropertyValue);
      if (ContainsKey(simpleKey)) {
        return EntityDictionary[simpleKey];
      }
      throw CreateReferencedEntityNotFoundException(
        ReferencingColumn.Name, simpleKey);
    }

    private void PopulateItems(
      // ReSharper disable once SuggestBaseTypeForParameter
      IEntityList entities) {
      Clear();
      if (entities is NewsletterList) {
        Add(new KeyValuePair<object, object>(
          Format(EntityBase.InitialDate.ToString(Global.DateFormat)),
          null));
      }
      AddRange(
        from IEntity entity in entities
        select (object)new KeyValuePair<object, object>(Format(entity.SimpleKey), entity)
      );
    }

    public static string ToSimpleKey([CanBeNull] object value) {
      switch (value) {
        case null:
          return null;
        case Newsletter newsletter:
          return EntityBase.DateToSimpleKey(newsletter.Date);
        case IEntity entity:
          return entity.SimpleKey;
        case DateTime date:
          return EntityBase.DateToSimpleKey(date);
        default:
          return value.ToString();
      }
    }
  }
}