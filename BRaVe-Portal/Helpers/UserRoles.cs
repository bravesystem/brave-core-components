using BRaVe_Portal.Models.Enums;
using System.Security.Claims;

namespace BRaVe_Portal.Helpers
{
    public static class UserRoles
    {
        /*public const string NoAuth = "NoAuth";
        public const string Admin = "Admin";
        public const string PA = "PA";
        public const string PM = "PM";
        public const string PO = "PO";
        public const string CoM = "CoM";
        public const string LEG = "LEG";
        public const string RO = "RO";
        public const string HQ = "HQ";*/

        public static string GetName(EnumUserRoles role)
        {
            return role.ToString();
        }

        public static string GetName(int id)
        {
            return EnumHelper.ToString<EnumUserRoles>(id);
        }


        internal static bool UserHasPendingJob(ClaimsPrincipal? user, AssessmentStatus status)
        {
            // short-circuit if no principal
            if (user is null) return false;

            // local helpers
            bool Is(string role) => user.IsInRole(role);

            switch (status)
            {
                case AssessmentStatus.Draft:
                case AssessmentStatus.PendingReview:
                case AssessmentStatus.LEG_Submitted:
                case AssessmentStatus.PM_ReviewInProgress:
                    return Is(EnumUserRoles.PM.ToString());

                case AssessmentStatus.Submitted:
                case AssessmentStatus.ReviewByPM:
                case AssessmentStatus.CoM_ReviewInProgress:
                case AssessmentStatus.CoM_FinalReviewInProgress:
                    return Is(EnumUserRoles.CoM.ToString());

                case AssessmentStatus.ReviewByCoM:
                case AssessmentStatus.ROHQ_ReviewInProgress:
                    return Is(EnumUserRoles.RO.ToString()) || Is(EnumUserRoles.HQ.ToString());

                case AssessmentStatus.ROHQ_Submitted:
                case AssessmentStatus.LEG_ReviewInProgress:
                    return Is(EnumUserRoles.LEG.ToString());

                default:
                    return false;
            }
        }




    }


    public enum EnumUserRoles
    {
        NoAuth=0,
        Admin =1,
        PA =7,
        PM =6,
        PO =8,
        CoM =2,
        LEG =5,
        RO =4,
        HQ =3,
        DA =10,
        DE =9,
    }
}
