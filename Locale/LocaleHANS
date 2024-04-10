//adapted from TreeController

namespace MapTextureReplacer.Locale
{
    using System.Collections.Generic;
    using Colossal;

    public class LocaleHANS : IDictionarySource
    {
        private readonly MapTextureReplacerOptions m_Setting;
        public LocaleHANS(MapTextureReplacerOptions options)
        {
            m_Setting = options;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "地图纹理替换" },
                { m_Setting.GetOptionLabelLocaleID(nameof(MapTextureReplacerOptions.ResetModSettings)), "重置所有设置" },
                { m_Setting.GetOptionDescLocaleID(nameof(MapTextureReplacerOptions.ResetModSettings)), "重置mod的所有设置，回到mod的初始默认值"},
                { m_Setting.GetOptionWarningLocaleID(nameof(MapTextureReplacerOptions.ResetModSettings)), "你确定要重置mod的所有设置?"}
            };

        }
        public void Unload()
        {
        }
    }
}
