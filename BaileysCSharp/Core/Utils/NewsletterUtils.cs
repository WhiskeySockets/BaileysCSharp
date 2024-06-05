using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaileysCSharp.Core.Utils
{
    public enum NewsletterReactionMode
    {
        ALL,
        BASIC,
        NONE,
    }

    public enum NewsletterVerification
    {
        VERIFIED,
        UNVERIFIED
    }


    public enum NewsletterState
    {
        ACTIVE,
        GEOSUSPENDED,
        SUSPENDED
    }

    public enum NewsletterMute
    {
        ON,
        OFF,
        UNDEFINED
    }

    public enum NewsletterViewRole
    {
        ADMIN,
        GUEST,
        OWNER,
        SUBSCRIBER,
    }
}
