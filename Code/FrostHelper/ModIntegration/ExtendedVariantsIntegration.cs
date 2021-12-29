using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostHelper.ModIntegration {
    public static class ExtendedVariantsIntegration {
        [OnLoadContent]
        public static void Load() {
            EverestModuleMetadata extVariantsModuleMeta = new EverestModuleMetadata { Name = "ExtendedVariantMode", VersionString = "0.19.8" };
            if (IntegrationUtils.TryGetModule(extVariantsModuleMeta, out extVariantsModule)) {
                Loaded = true;
                ExtVariantsModule_TriggerManager = extVariantsModule.GetType().GetField("TriggerManager");
                ExtVariantsModule_VariantHandlers = extVariantsModule.GetType().GetField("VariantHandlers");
                variantsEnumType = extVariantsModule.GetType().Module.GetType("ExtendedVariants.Module.ExtendedVariantsModule+Variant");
                TriggerManager_OnEnteredInTrigger = ExtVariantsModule_TriggerManager.FieldType.GetMethod("OnEnteredInTrigger", new[] {
                    variantsEnumType, typeof(int), typeof(bool),
                });
                JumpCountVariant = Enum.Parse(variantsEnumType, "JumpCount");
            }
        }

        public static bool Loaded;
        private static EverestModule extVariantsModule;
        private static FieldInfo ExtVariantsModule_TriggerManager;
        private static FieldInfo ExtVariantsModule_VariantHandlers;
        private static MethodInfo TriggerManager_OnEnteredInTrigger;
        private static Type variantsEnumType;
        private static object JumpCountVariant;

        public static object GetTriggerManager() {
            return ExtVariantsModule_TriggerManager.GetValue(extVariantsModule);
        }

        public static int GetCurrentJumpCountVariantValue() {
            var triggerManager = GetTriggerManager();

            return (int)triggerManager.Invoke("GetCurrentVariantValue", JumpCountVariant);
        }

        public static void SetCurrentJumpCountVariantValue(int amt) {
            var triggerManager = GetTriggerManager();
            TriggerManager_OnEnteredInTrigger.Invoke(triggerManager, new[] { JumpCountVariant, amt, false });

            IDictionary dictionary = (IDictionary)ExtVariantsModule_VariantHandlers.GetValue(extVariantsModule);
            var jumpCountVariant = new DynamicData(dictionary[JumpCountVariant]);

            jumpCountVariant.Invoke("AddJumps", amt, true, -1);
        }

    }
}
