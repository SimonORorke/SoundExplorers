using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using SoundExplorers.Data;
using VelocityDb.Session;

namespace SoundExplorers.Model {
  /// <summary>
  ///   Accesses an option/preference for the current user,
  ///   as held on the UserOption table.
  /// </summary>
  public class Option {

    /// <summary>
    ///   Initialises a new instance of the Option class,
    ///   fetching the corresponding UserOption record if it already exists.
    /// </summary>
    /// <param name="name">
    ///   The name that identifies the option relative to the current user.
    /// </param>
    /// <param name="defaultValue">
    ///   Default value for the option if not found on the database.
    ///   If not specified, the default string value will be an empty string,
    ///   the default boolean value will be False
    ///   and the default integer value will be zero.
    /// </param>
    /// <remarks>
    ///   The <see cref="StringValue" /> property will be set to
    ///   the current value of the <b>OptionValue</b> field of the
    ///   <b>UserOption</b> record, if found.
    ///   Otherwise the <see cref="StringValue" /> property will be set to
    ///   to the value of the <paramref name="defaultValue" /> parameter,
    ///   if specified or, failing that, to an empty string.
    /// </remarks>
    /// <exception cref="DataException">
    ///   Thrown if
    ///   there is an error on attempting to access the database.
    /// </exception>
    [ExcludeFromCodeCoverage]
    public Option([NotNull] string name, object defaultValue = null) : this(
      QueryHelper.Instance, Global.Session, name, defaultValue) { }

    /// <summary>
    ///   Should only need to be called directly for testing.
    /// </summary>
    protected Option([NotNull] QueryHelper queryHelper, [NotNull] SessionBase session,
      [NotNull] string name, object defaultValue = null) {
      Session = session;
      DefaultValue = defaultValue;
      var temp = new UserOption {UserId = Environment.UserName, OptionName = name};
      Session.BeginUpdate();
      UserOption = queryHelper.Find<UserOption>(temp.SimpleKey, Session);
      if (UserOption == null) {
        UserOption = temp;
        UserOption.OptionValue = !string.IsNullOrWhiteSpace(DefaultValue?.ToString())
          ? DefaultValue.ToString()
          : string.Empty;
      }
      Session.Commit();
    }
    
    private SessionBase Session { get; }

    /// <summary>
    ///   Gets or sets the entity that represents the
    ///   data for the UserOption database record.
    /// </summary>
    private UserOption UserOption { get; }

    /// <summary>
    ///   Gets or sets the current value of the option as a boolean.
    /// </summary>
    /// <remarks>
    ///   This is initially value of the <b>OptionValue</b> field of the
    ///   corresponding <b>UserOption</b> record, if it exists
    ///   and <b>OptionValue</b> contains a valid integer.
    ///   Otherwise it will initially be False or the default value
    ///   that can optionally be set in the constructor.
    ///   <para>
    ///     When set, the database will be updated unless the there's
    ///     no actual change to the previous value.
    ///     The corresponding <b>UserOption</b> record will be updated if found
    ///     or inserted if not.
    ///   </para>
    /// </remarks>
    public bool BooleanValue {
      get {
        bool.TryParse(StringValue, out bool result);
        return result;
      }
      set => StringValue = value.ToString();
    }

    /// <summary>
    ///   Default value.
    /// </summary>
    private object DefaultValue { get; }

    /// <summary>
    ///   Gets or sets the current value of the option as an integer.
    /// </summary>
    /// <remarks>
    ///   This is initially value of the <b>OptionValue</b> field of the
    ///   corresponding <b>UserOption</b> record, if it exists
    ///   and <b>OptionValue</b> contains a valid integer.
    ///   Otherwise it will initially be zero or the default value
    ///   that can optionally be set in the constructor.
    ///   <para>
    ///     When set, the database will be updated unless the there's
    ///     no actual change to the previous value.
    ///     The corresponding <b>UserOption</b> record will be updated if found
    ///     or inserted if not.
    ///   </para>
    /// </remarks>
    public int Int32Value {
      get {
        int.TryParse(StringValue, out int result);
        return result;
      }
      set => StringValue = value.ToString();
    }

    /// <summary>
    ///   Gets or sets the current value of the option as a string.
    /// </summary>
    /// <remarks>
    ///   This is initially value of the <b>OptionValue</b> field of the
    ///   corresponding <b>UserOption</b> record, if it exists.
    ///   Otherwise it will initially be blank or the default value
    ///   that can optionally be set in the constructor.
    ///   <para>
    ///     When set, the database will be updated unless the there's
    ///     no actual change to the previous value.
    ///     When set to a value other than a null reference or an empty string,
    ///     the corresponding <b>UserOption</b> record will be updated if found
    ///     or inserted if not.
    ///     When set to a null reference or an empty string,
    ///     the the corresponding <b>UserOption</b> record will be
    ///     will be deleted if found.
    ///   </para>
    /// </remarks>
    public string StringValue {
      get => UserOption.OptionValue;
      set {
        if (value == UserOption.OptionValue) {
          return;
        }
        Session.BeginUpdate();
        if (!string.IsNullOrWhiteSpace(value)) {
          UserOption.OptionValue = value;
          if (!UserOption.IsPersistent) {
            Session.Persist(UserOption);
          }
        } else if (UserOption.IsPersistent) {
          Session.Unpersist(UserOption);
          UserOption.OptionValue = value;
        }
        Session.Commit();
      }
    }
  } //End of class
} //End of namespace