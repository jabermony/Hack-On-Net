using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Util;

namespace HackLinks_Server.Computers.Permissions
{
    static class PermissionHelper
    {
        /// <summary>
        /// Returns the <see cref="Group"/> for the given string or <see cref="Group.INVALID"/> if no match was found.
        /// </summary>
        /// <param name="groupString"></param>
        /// <returns>The matching Group or INVALID if no match</returns>
        public static Group GetGroupFromString(string groupString)
        {
            if (!Enum.TryParse(groupString.ToUpper(), out Group group) || !Enum.IsDefined(typeof(Group), group))
            {
                return Group.INVALID;
            }
            return group;
        }

        public static bool ApplyModifiers(string modifer, Permission permissionValue, out Permission outValue)
        {
            bool ret = ApplyModifiers(modifer, (int)permissionValue, out int outInt);
            outValue = (Permission) outInt;
            return ret;
        }

        public static bool ApplyModifiers(string modifer, int permissionValue, out int outValue)
        {
            // We've set it to an entirely new value if this matches
            if (Regex.IsMatch(modifer, "^[0-7]{1,3}$"))
            {
                // file mode is specified in octal
                outValue = Convert.ToInt32(modifer, 8);
                return true;
            }

            Match match = Regex.Match(modifer, "^([augo]*)([+=-][rwx]*)+$");
            if(match.Success && match.Groups.Count >= 2)
            {
                // Initialize our new value to the current permission value
                int newValue = permissionValue;

                string permissionTypeChars = match.Groups[1].Captures[0].Value;

                HashSet<PermissionType> permissionTypes = new HashSet<PermissionType>();

                int lengthOfTypes = Enum.GetValues(typeof(PermissionType)).Length;

                if(permissionTypeChars.Length > 0)
                {
                    foreach (char permissionTypeChar in permissionTypeChars)
                    {
                        if (permissionTypes.Count > lengthOfTypes)
                        {
                            break;
                        }
                        switch (permissionTypeChar)
                        {
                            case 'u':
                                permissionTypes.Add(PermissionType.User);
                                continue;
                            case 'g':
                                permissionTypes.Add(PermissionType.Group);
                                continue;
                            case 'o':
                                permissionTypes.Add(PermissionType.Others);
                                continue;
                            case 'a':
                                permissionTypes.Add(PermissionType.All);
                                continue;
                            default:
                                throw new InvalidOperationException($"Invalid permission Type '{permissionTypeChar}'");
                        }
                    }
                }
                else
                {
                    //No type specified, default to all types
                    permissionTypes.Add(PermissionType.All);
                }

                foreach(Capture permissionCapture in match.Groups[2].Captures)
                {
                    string permission = permissionCapture.Value;
                    Logger.Debug($"permission {permission}");

                    Permission newPermission = 0;
                    if (permission.Contains('r'))
                        newPermission |= Permission.A_Read;
                    if (permission.Contains('w'))
                        newPermission |= Permission.A_Write;
                    if (permission.Contains('x'))
                        newPermission |= Permission.A_Execute;

                    foreach (PermissionType type in permissionTypes)
                    {
                        switch (permission[0])
                        {
                            case '=':
                                // we clear the given permission type first
                                newValue &= ~(int)type;
                                // we then add the new permissions
                                goto case '+';
                            case '+':
                                // apply our digit to the given type
                                newValue |= ((int)newPermission & (int)type);
                                break;
                            case '-':
                                newValue &= ~(int)newPermission;
                                break;
                            // Logically there will never be anything but a '+', '=', or '-' here, so default throws exceptions in case of future bugs.
                            default:
                                throw new InvalidOperationException($"Invalid permission Modifier '{permission[0]}'");
                        }
                    }
                }

                outValue = newValue;
                return true;
            }

            outValue = permissionValue;
            return false;
        }

        /// <summary>
        /// <para>Check if the file has permission for the given operations for the given type.</para>
        /// </summary>
        /// <param name="value">The permission as a digit calculated from <see cref="Permission"/>. To calculate the value do a bitwise OR for the value of the required.</param>
        /// <returns>True if the type would have permission to perform the operation, false otherwise</returns>
        public static bool CheckPermission(Permission value, Permission permission, int fileOwnerId, Group fileGroup, int userId, params Group[] privs)
        {
            if (fileOwnerId != userId)
            {
                permission &= ~Permission.O_All;
            }

            if (!privs.Contains(fileGroup))
            {
                permission &= ~Permission.G_All;
            }

            return value == (permission & value);
        }

        public static bool CheckCredentials(Credentials credentials, int UserId, Group targetGroup)
        {
            if(credentials.UserId == UserId)
            {
                return CheckCredentials(credentials, targetGroup);
            }
            return false;
        }

        public static bool CheckCredentials(Credentials credentials, Group targetGroup)
        {
            if (credentials.Group.Equals(targetGroup))
            {
                return true;
            }
            else if (credentials.Groups.Contains(targetGroup))
            {
                return true;
            }
            return false;
        }

        public static string PermissionToDisplayString(Permission permissionValue)
        {
            return PermissionToDisplayString((int)permissionValue);
        }

        public static string PermissionToDisplayString(int permissionValue)
        {
            StringBuilder output = new StringBuilder();
            if((permissionValue & (int) PermissionType.User) != 0)
            {
                if(output.Length > 0)
                {
                    output.Append(", ");
                }
                output.Append("U=");
                if (CheckValue(permissionValue, Permission.U_Read)) output.Append('R');
                else output.Append('-');
                if (CheckValue(permissionValue, Permission.U_Write)) output.Append('W');
                else output.Append('-');
                if (CheckValue(permissionValue, Permission.U_Execute)) output.Append('X');
                else output.Append('-');
            }

            if ((permissionValue & (int)PermissionType.Group) != 0)
            {
                if (output.Length > 0)
                {
                    output.Append(", ");
                }
                output.Append("G=");
                if (CheckValue(permissionValue, Permission.G_Read)) output.Append('R');
                else output.Append('-');
                if (CheckValue(permissionValue, Permission.G_Write)) output.Append('W');
                else output.Append('-');
                if (CheckValue(permissionValue, Permission.G_Execute)) output.Append('X');
                else output.Append('-');
            }

            if ((permissionValue & (int)PermissionType.Others) != 0)
            {
                if (output.Length > 0)
                {
                    output.Append(", ");
                }
                output.Append("O=");
                if (CheckValue(permissionValue, Permission.O_Read)) output.Append('R');
                else output.Append('-');
                if (CheckValue(permissionValue, Permission.O_Write)) output.Append('W');
                else output.Append('-');
                if (CheckValue(permissionValue, Permission.O_Execute)) output.Append('X');
                else output.Append('-');
            }

            return output.ToString();
        }

        private static bool CheckValue(int value, Permission permission)
        {
            return ((value & (int)permission) == (int)permission);
        }
    }
}
