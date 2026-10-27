using System;
using Microsoft.Win32;
using TavrynexoAntiSpy.Models;

namespace TavrynexoAntiSpy.Services
{
    public class RegistryOperationResult
    {
        public bool Success { get; set; }
        public object? PreviousValue { get; set; }
        public object? AppliedValue { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public static class RegistryManager
    {
        private static RegistryKey GetBaseHive(RegistryHiveType hive)
        {
            return hive switch
            {
                RegistryHiveType.LocalMachine => Registry.LocalMachine,
                RegistryHiveType.CurrentUser => Registry.CurrentUser,
                _ => Registry.LocalMachine
            };
        }

        public static object? ReadValue(RegistryHiveType hive, string subKeyPath, string valueName)
        {
            try
            {
                using var baseHive = GetBaseHive(hive);
                using var key = baseHive.OpenSubKey(subKeyPath, writable: false);
                return key?.GetValue(valueName);
            }
            catch
            {
                return null;
            }
        }

        public static RegistryOperationResult WriteAndVerify(
            RegistryHiveType hive, 
            string subKeyPath, 
            string valueName, 
            object desiredValue, 
            RegistryValueKind kind = RegistryValueKind.DWord)
        {
            var result = new RegistryOperationResult();
            try
            {
                using var baseHive = GetBaseHive(hive);
                using (var existingKey = baseHive.OpenSubKey(subKeyPath, writable: false))
                {
                    result.PreviousValue = existingKey?.GetValue(valueName);
                }

                using (var key = baseHive.CreateSubKey(subKeyPath, writable: true))
                {
                    if (key == null)
                    {
                        result.Success = false;
                        result.Message = $"Access denied to: {subKeyPath}";
                        return result;
                    }
                    key.SetValue(valueName, desiredValue, kind);
                    key.Flush();
                }

                using (var verifyKey = baseHive.OpenSubKey(subKeyPath, writable: false))
                {
                    var actualValue = verifyKey?.GetValue(valueName);
                    result.AppliedValue = actualValue;
                    result.Success = actualValue != null && actualValue.ToString() == desiredValue.ToString();
                    result.Message = result.Success ? $"Verified write: [{valueName} = {actualValue}]" : "Verification mismatch";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        public static RegistryOperationResult RevertValue(
            RegistryHiveType hive,
            string subKeyPath,
            string valueName,
            object? originalValue,
            RegistryValueKind kind = RegistryValueKind.DWord)
        {
            var result = new RegistryOperationResult();
            try
            {
                using var baseHive = GetBaseHive(hive);
                if (originalValue == null)
                {
                    using var key = baseHive.OpenSubKey(subKeyPath, writable: true);
                    key?.DeleteValue(valueName, throwOnMissingValue: false);
                }
                else
                {
                    return WriteAndVerify(hive, subKeyPath, valueName, originalValue, kind);
                }
                result.Success = true;
                result.Message = "Reverted successfully.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }
    }
}
