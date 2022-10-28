﻿using System;
using System.Globalization;
using System.Text;
using Starlight.Apis.JoinGame;
using Starlight.PostLaunch;

namespace Starlight.Launch;

public class LaunchParams : IRobloxLaunchParams, IStarlightLaunchParams
{
    JoinRequest _request;

    public long? TrackerId
    {
        get => Request?.BrowserTrackerId;
        set => Request.BrowserTrackerId = value;
    }

    public CultureInfo RobloxLocale { get; set; } = CultureInfo.CurrentCulture;

    public CultureInfo GameLocale { get; set; } = CultureInfo.CurrentCulture;

    public string Ticket { get; set; }

    public DateTimeOffset LaunchTime { get; set; } = DateTime.Now;

    public JoinRequest Request
    {
        get => _request;
        set
        {
            if (TrackerId is not null)
                value.BrowserTrackerId = TrackerId;
            _request = value;
        }
    }

    public int FpsCap { get; set; }

    public bool Headless { get; set; }

    public bool Spoof { get; set; }

    public string Hash { get; set; }

    public string Resolution { get; set; }

    public AttachMethod AttachMethod { get; set; }

    public void Merge<T>(T args)
    {
        foreach (var member in typeof(T).GetProperties())
        {
            var value = member.GetValue(args);
            member.SetValue(this, value);
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(Ticket))
            sb.Append("Ticket=\"REDACTED-FOR-PRIVACY\";"); // you're welcome :)

        foreach (var prop in GetType().GetProperties())
        {
            if (!prop.CanRead || prop.Name is "Ticket" or "Request")
                continue;

            var defaultValue = prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null;
            var value = prop.GetValue(this);

            if (value != defaultValue)
                sb.Append(prop.Name + $"=\"{value}\";");
        }

        return sb.ToString();
    }
}