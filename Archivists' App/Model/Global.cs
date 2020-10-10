﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using SoundExplorers.Data;
using VelocityDb.Session;

namespace SoundExplorers.Model {
  public static class Global {
    public static SessionBase Session { get; set; }

    /// <summary>
    ///   Creates an instance of the specified entity list type.
    /// </summary>
    /// <param name="type">
    ///   The type of entity list to be created.
    /// </param>
    [NotNull]
    public static IEntityList CreateEntityList([NotNull] Type type) {
      try {
        return (IEntityList)Activator.CreateInstance(type);
      } catch (TargetInvocationException ex) {
        throw ex.InnerException ?? ex;
      }
    }

    /// <summary>
    ///   Creates a sorted dictionary of entity list types,
    ///   with the entity name as the key
    ///   and the type as the value.
    /// </summary>
    /// <returns>
    ///   The sorted dictionary created.
    /// </returns>
    [NotNull]
    public static SortedDictionary<string, Type> CreateEntityListTypeDictionary() {
      return new SortedDictionary<string, Type> {
        //{nameof(Event), typeof(EventList)},
        {nameof(EventType), typeof(EventTypeList)},
        {nameof(Location), typeof(LocationList)},
        {nameof(Newsletter), typeof(NewsletterList)},
        {nameof(Series), typeof(SeriesList)},
        // {nameof(Set), typeof(SetList)}
      };
    }

    public static Exception CreateFileException(
      Exception exception,
      string pathDescription,
      string path) {
      switch (exception) {
        case IOException _:
        case UnauthorizedAccessException _:
          return new ApplicationException(
            $"{pathDescription}:{Environment.NewLine}{exception.Message}");
        case NotSupportedException _:
          return new ApplicationException(
            pathDescription + " \""
                            + path
                            + "\" is not a properly formatted file path.");
        default:
          return exception;
      }
    }

    public static string GetApplicationFolderPath() {
      return Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
    }
  }
}