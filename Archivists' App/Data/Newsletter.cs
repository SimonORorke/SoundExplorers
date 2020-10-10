﻿using System;
using System.Collections;
using System.Data;
using System.Data.Linq;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using VelocityDb.Session;

namespace SoundExplorers.Data {
  /// <summary>
  ///   An entity representing a newsletter that documents one or more Events.
  /// </summary>
  public class Newsletter : EntityBase {
    private DateTime _date;
    private string _url;

    public Newsletter() : base(typeof(Newsletter), nameof(Date), null) {
      _date = InitialDate;
      Events = new SortedChildList<Event>(this);
    }

    /// <summary>
    ///   The newsletter's publication date (email send date).
    /// </summary>
    /// <exception cref="NoNullAllowedException"></exception>
    public DateTime Date {
      get => _date;
      set {
        if (value <= InitialDate) {
          throw new NoNullAllowedException(
            $"Newsletter Date must be later than {InitialDate:yyyy/mm/dd}.");
        }
        UpdateNonIndexField();
        _date = value.Date;
        SimpleKey = $"{_date:yyyy/MM/dd}";
      }
    }

    [NotNull] public SortedChildList<Event> Events { get; }

    /// <summary>
    ///   The URL where the newsletter is archived.
    ///   Must be specified and unique.
    /// </summary>
    public string Url {
      get => _url;
      set {
        CheckCanChangeUrl(_url, value);
        UpdateNonIndexField();
        _url = value;
      }
    }

    private void CheckCanChangeUrl([CanBeNull] string oldUrl,
      [CanBeNull] string newUrl) {
      if (newUrl == null) {
        throw new NoNullAllowedException(
          $"A valid URL has not been specified for Newsletter '{SimpleKey}'.");
      }
      try {
        var dummy = new Uri(newUrl, UriKind.Absolute);
      } catch (UriFormatException) {
        throw new FormatException(
          $"Newsletter '{SimpleKey}': invalid URL format '{newUrl}'.");
      }
      if (IsPersistent && Session != null && newUrl != oldUrl) {
        // If there's no session, which means we cannot check for a duplicate,
        // EntityBase.UpdateNonIndexField will throw 
        // an InvalidOperationException anyway.
        var duplicate = FindDuplicateUrl(newUrl, Session);
        if (duplicate != null) {
          throw new DuplicateKeyException(
            this,
            $"The URL of Newsletter '{SimpleKey}' cannot be set to " +
            $"'{newUrl}'. Newsletter {duplicate.SimpleKey} " +
            "has already been persisted with that URL.");
        }
      }
    }

    protected override void CheckCanPersist(SessionBase session) {
      base.CheckCanPersist(session);
      if (Url == null) {
        throw new NoNullAllowedException(
          $"Newsletter '{SimpleKey}' " +
          "cannot be persisted because a URL has not been specified.");
      }
      var urlDuplicate = FindDuplicateUrl(Url, session);
      if (urlDuplicate != null) {
        throw new DuplicateKeyException(
          this,
          $"Newsletter '{SimpleKey}' " +
          "cannot be persisted because Newsletter " +
          $"'{urlDuplicate.SimpleKey}' " +
          $"has already been persisted with the same URL '{Url}'.");
      }
    }

    [CanBeNull]
    private Newsletter FindDuplicateUrl([NotNull] string url,
      [NotNull] SessionBase session) {
      return QueryHelper.Find<Newsletter>(
        newsletter => newsletter.Url.Equals(url) && !newsletter.Oid.Equals(Oid),
        session);
    }

    protected override IDictionary GetChildren(Type childType) {
      return Events;
    }

    [ExcludeFromCodeCoverage]
    protected override void SetNonIdentifyingParentField(
      Type parentEntityType, EntityBase newParent) {
      throw new NotSupportedException();
    }
  }
}