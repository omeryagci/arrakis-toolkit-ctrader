// Arrakis Toolkit — cTrader sürümü (kişisel kullanım)
// TradingView Pine sürümünün C# karşılığı. Bu gösterge sadece çizim yapar:
// hiçbir emir, pozisyon veya hesap işlemi çağırmaz ve hiçbir izin istemez (AccessRights.None).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo.Indicators
{
    public enum Gorunum { Temiz, Tam }
    public enum Secim { Otomatik, Elle }
    public enum Cizgi { Düz, Kesikli, Noktalı }
    public enum Piyasa { Otomatik, Forex, Endeks }
    public enum YaziBoyu { Çok_küçük, Küçük, Normal }
    public enum WmBoyu { Çok_küçük, Küçük, Normal, Büyük, Çok_büyük }
    public enum Konum { Sol, Orta, Sağ }
    public enum Alinan { İkisi, Soluklaştır, Çarpı, Kapalı }
    public enum Tur { Saat_ve_günler, Aylık, Üç_aylık, Yıllık }
    public enum YaziTipi { Normal, Monospace }

    [Indicator(IsOverlay = true, TimeZone = TimeZones.EasternStandardTime, AccessRights = AccessRights.None, AutoRescale = false)]
    public class ArrakisToolkit : Indicator
    {
        // ───────────────────────── SMT ─────────────────────────
        const string G_SMT = "SMT";
        [Parameter("Ana grafikte SMT", DefaultValue = true, Group = G_SMT, Description = "Otomatik: 1m grafikte 15m, 5m'de 1h, 15m'de 4h, 1h ve 4h'te 1D.")] public bool SmtOn { get; set; }
        [Parameter("Zaman dilimi", DefaultValue = Secim.Otomatik, Group = G_SMT)] public Secim SmtTfSel { get; set; }
        [Parameter("Elle zaman dilimi", DefaultValue = "Hour", Group = G_SMT)] public TimeFrame SmtTfManual { get; set; }
        [Parameter("Karşı sembol", DefaultValue = Secim.Otomatik, Group = G_SMT, Description = "Otomatik: EURUSD↔GBPUSD, US100↔US500, XAUUSD↔XAGUSD gibi. Eşleşme yoksa SMT çizilmez; o zaman Elle seç.")] public Secim SmtSymSel { get; set; }
        [Parameter("Elle karşı sembol", DefaultValue = "GBPUSD", Group = G_SMT)] public string SmtSymManual { get; set; }
        [Parameter("Elle seçilen sembol ters ilişkili", DefaultValue = false, Group = G_SMT)] public bool SmtInvManual { get; set; }
        [Parameter("Swing gücü (mum)", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = G_SMT)] public int SmtP { get; set; }
        [Parameter("Bullish renk", DefaultValue = "#1D4ED8", Group = G_SMT)] public Color SmtBull { get; set; }
        [Parameter("Bearish renk", DefaultValue = "#C93800", Group = G_SMT)] public Color SmtBear { get; set; }
        [Parameter("En fazla", DefaultValue = 5, MinValue = 1, MaxValue = 20, Group = G_SMT, Description = "Yön başına en yeni uyumsuzluklar çizilir.")] public int SmtMax { get; set; }

        // ───────────────────────── Genel ─────────────────────────
        const string G_GEN = "Genel";
        [Parameter("Görünüm", DefaultValue = Gorunum.Temiz, Group = G_GEN, Description = "Temiz: hiçbir şey kaybolmaz, geçmiş günler ve uzak üst zaman dilimi FVG'leri soluklaşır. Tam: hepsi aynı güçte çizilir.")] public Gorunum View { get; set; }
        [Parameter("Kutuları göster", DefaultValue = true, Group = G_GEN)] public bool AllOn { get; set; }
        [Parameter("Kutular en fazla bu zaman diliminde", DefaultValue = "Hour", Group = G_GEN)] public TimeFrame MaxTf { get; set; }
        [Parameter("Seviye uzama sınırı (işlem günü)", DefaultValue = 2, MinValue = 1, MaxValue = 20, Group = G_GEN)] public int ExtDays { get; set; }
        [Parameter("Seviye yazı boyutu", DefaultValue = YaziBoyu.Normal, Group = G_GEN)] public YaziBoyu LblSize { get; set; }
        [Parameter("Alınan seviye", DefaultValue = Alinan.İkisi, Group = G_GEN, Description = "Fitil değen seviye soluklaşır ve/veya değdiği yere ✕ konur.")] public Alinan Taken { get; set; }
        [Parameter("Hesaplanan geçmiş (bar)", DefaultValue = 8000, MinValue = 500, MaxValue = 50000, Group = G_GEN, Description = "Grafiğin son kaç mumu işlenir. Büyüdükçe daha eski günler çizilir ama gösterge yavaşlar.")] public int MaxBars { get; set; }

        // ───────────────────────── Kutular ─────────────────────────
        const string G1 = "Kutu 1", G2 = "Kutu 2", G3 = "Kutu 3", G4 = "Kutu 4";
        [Parameter("Göster", DefaultValue = true, Group = G1)] public bool K1On { get; set; }
        [Parameter("İsim", DefaultValue = "AS", Group = G1)] public string K1Name { get; set; }
        [Parameter("Saat (NY)", DefaultValue = "20:00-00:00", Group = G1)] public string K1Sess { get; set; }
        [Parameter("Dolgu", DefaultValue = false, Group = G1)] public bool K1Fill { get; set; }
        [Parameter("Dolgu rengi", DefaultValue = "#262E4A8C", Group = G1)] public Color K1FillCol { get; set; }
        [Parameter("Kenar", DefaultValue = true, Group = G1)] public bool K1Bord { get; set; }
        [Parameter("Kenar rengi", DefaultValue = "#2E4A8C", Group = G1)] public Color K1BordCol { get; set; }
        [Parameter("Kenar stili", DefaultValue = Cizgi.Kesikli, Group = G1)] public Cizgi K1BordSty { get; set; }
        [Parameter("Seviye", DefaultValue = true, Group = G1)] public bool K1Lvl { get; set; }
        [Parameter("Seviye rengi", DefaultValue = "#2E4A8C", Group = G1)] public Color K1LvlCol { get; set; }
        [Parameter("Seviye stili", DefaultValue = Cizgi.Kesikli, Group = G1)] public Cizgi K1LvlSty { get; set; }
        [Parameter("Seviye kalınlığı", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = G1)] public int K1LvlW { get; set; }

        [Parameter("Göster", DefaultValue = true, Group = G2)] public bool K2On { get; set; }
        [Parameter("İsim", DefaultValue = "LO", Group = G2)] public string K2Name { get; set; }
        [Parameter("Saat (NY)", DefaultValue = "02:00-05:00", Group = G2)] public string K2Sess { get; set; }
        [Parameter("Dolgu", DefaultValue = false, Group = G2)] public bool K2Fill { get; set; }
        [Parameter("Dolgu rengi", DefaultValue = "#261E7F78", Group = G2)] public Color K2FillCol { get; set; }
        [Parameter("Kenar", DefaultValue = true, Group = G2)] public bool K2Bord { get; set; }
        [Parameter("Kenar rengi", DefaultValue = "#1E7F78", Group = G2)] public Color K2BordCol { get; set; }
        [Parameter("Kenar stili", DefaultValue = Cizgi.Kesikli, Group = G2)] public Cizgi K2BordSty { get; set; }
        [Parameter("Seviye", DefaultValue = true, Group = G2)] public bool K2Lvl { get; set; }
        [Parameter("Seviye rengi", DefaultValue = "#1E7F78", Group = G2)] public Color K2LvlCol { get; set; }
        [Parameter("Seviye stili", DefaultValue = Cizgi.Kesikli, Group = G2)] public Cizgi K2LvlSty { get; set; }
        [Parameter("Seviye kalınlığı", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = G2)] public int K2LvlW { get; set; }

        [Parameter("Göster", DefaultValue = true, Group = G3)] public bool K3On { get; set; }
        [Parameter("İsim", DefaultValue = "NY AM", Group = G3)] public string K3Name { get; set; }
        [Parameter("Forex saati (NY)", DefaultValue = "07:00-10:00", Group = G3)] public string K3SessF { get; set; }
        [Parameter("Endeks saati (NY)", DefaultValue = "08:30-11:00", Group = G3)] public string K3SessI { get; set; }
        [Parameter("Piyasa", DefaultValue = Piyasa.Otomatik, Group = G3, Description = "Otomatik: forex paritelerinde Forex saati; endeks, altın, gümüş ve diğer tüm sembollerde Endeks saati kullanılır.")] public Piyasa K3Mode { get; set; }
        [Parameter("Dolgu", DefaultValue = false, Group = G3)] public bool K3Fill { get; set; }
        [Parameter("Dolgu rengi", DefaultValue = "#265B2A4E", Group = G3)] public Color K3FillCol { get; set; }
        [Parameter("Kenar", DefaultValue = true, Group = G3)] public bool K3Bord { get; set; }
        [Parameter("Kenar rengi", DefaultValue = "#5B2A4E", Group = G3)] public Color K3BordCol { get; set; }
        [Parameter("Kenar stili", DefaultValue = Cizgi.Kesikli, Group = G3)] public Cizgi K3BordSty { get; set; }
        [Parameter("Seviye", DefaultValue = true, Group = G3)] public bool K3Lvl { get; set; }
        [Parameter("Seviye rengi", DefaultValue = "#5B2A4E", Group = G3)] public Color K3LvlCol { get; set; }
        [Parameter("Seviye stili", DefaultValue = Cizgi.Kesikli, Group = G3)] public Cizgi K3LvlSty { get; set; }
        [Parameter("Seviye kalınlığı", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = G3)] public int K3LvlW { get; set; }

        [Parameter("Göster", DefaultValue = true, Group = G4)] public bool K4On { get; set; }
        [Parameter("İsim", DefaultValue = "CBDR", Group = G4)] public string K4Name { get; set; }
        [Parameter("Saat (NY)", DefaultValue = "14:00-20:00", Group = G4)] public string K4Sess { get; set; }
        [Parameter("Dolgu", DefaultValue = false, Group = G4)] public bool K4Fill { get; set; }
        [Parameter("Dolgu rengi", DefaultValue = "#266B6560", Group = G4)] public Color K4FillCol { get; set; }
        [Parameter("Kenar", DefaultValue = true, Group = G4)] public bool K4Bord { get; set; }
        [Parameter("Kenar rengi", DefaultValue = "#6B6560", Group = G4)] public Color K4BordCol { get; set; }
        [Parameter("Kenar stili", DefaultValue = Cizgi.Kesikli, Group = G4)] public Cizgi K4BordSty { get; set; }
        [Parameter("Seviye", DefaultValue = true, Group = G4)] public bool K4Lvl { get; set; }
        [Parameter("Seviye rengi", DefaultValue = "#6B6560", Group = G4)] public Color K4LvlCol { get; set; }
        [Parameter("Seviye stili", DefaultValue = Cizgi.Kesikli, Group = G4)] public Cizgi K4LvlSty { get; set; }
        [Parameter("Seviye kalınlığı", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = G4)] public int K4LvlW { get; set; }

        // ───────────────────────── Ayırıcılar ─────────────────────────
        const string G_SEP = "Ayırıcılar";
        [Parameter("Sıradaki ayırıcıyı göster", DefaultValue = true, Group = G_SEP)] public bool SepFut { get; set; }
        [Parameter("Ayırıcı yazı boyutu", DefaultValue = YaziBoyu.Normal, Group = G_SEP)] public YaziBoyu SepLbl { get; set; }

        const string G_H = "Saatlik ayırıcı";
        [Parameter("Göster", DefaultValue = true, Group = G_H)] public bool HOn { get; set; }
        [Parameter("Yazı", DefaultValue = "", Group = G_H)] public string HTxt { get; set; }
        [Parameter("Renk", DefaultValue = "#808C7A5B", Group = G_H)] public Color HCol { get; set; }
        [Parameter("Stil", DefaultValue = Cizgi.Kesikli, Group = G_H)] public Cizgi HSty { get; set; }
        [Parameter("Kalınlık", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = G_H)] public int HW { get; set; }
        [Parameter("En fazla bu zaman diliminde", DefaultValue = "Minute15", Group = G_H)] public TimeFrame HTf { get; set; }

        const string G_M = "09:30 makro ayırıcı";
        [Parameter("Göster", DefaultValue = true, Group = G_M)] public bool MOn { get; set; }
        [Parameter("Yazı", DefaultValue = "", Group = G_M)] public string MTxt { get; set; }
        [Parameter("Renk", DefaultValue = "#809C6B12", Group = G_M)] public Color MCol { get; set; }
        [Parameter("Stil", DefaultValue = Cizgi.Kesikli, Group = G_M)] public Cizgi MSty { get; set; }
        [Parameter("Kalınlık", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = G_M)] public int MW { get; set; }
        [Parameter("En fazla bu zaman diliminde", DefaultValue = "Minute15", Group = G_M)] public TimeFrame MTf { get; set; }

        const string G_D = "Günlük ayırıcı";
        [Parameter("Göster", DefaultValue = true, Group = G_D)] public bool DOn { get; set; }
        [Parameter("Yazı", DefaultValue = "", Group = G_D)] public string DTxt { get; set; }
        [Parameter("Renk", DefaultValue = "#6B6560", Group = G_D)] public Color DCol { get; set; }
        [Parameter("Stil", DefaultValue = Cizgi.Noktalı, Group = G_D)] public Cizgi DSty { get; set; }
        [Parameter("Kalınlık", DefaultValue = 2, MinValue = 1, MaxValue = 4, Group = G_D)] public int DW { get; set; }
        [Parameter("En fazla bu zaman diliminde", DefaultValue = "Hour4", Group = G_D)] public TimeFrame DTf { get; set; }

        const string G_W = "Haftalık ayırıcı";
        [Parameter("Göster", DefaultValue = true, Group = G_W)] public bool WOn { get; set; }
        [Parameter("Yazı", DefaultValue = "", Group = G_W)] public string WTxt { get; set; }
        [Parameter("Renk", DefaultValue = "#805B2A4E", Group = G_W)] public Color WCol { get; set; }
        [Parameter("Stil", DefaultValue = Cizgi.Düz, Group = G_W)] public Cizgi WSty { get; set; }
        [Parameter("Kalınlık", DefaultValue = 2, MinValue = 1, MaxValue = 4, Group = G_W)] public int WW { get; set; }
        [Parameter("En fazla bu zaman diliminde", DefaultValue = "Daily", Group = G_W)] public TimeFrame WTf { get; set; }

        const string GX1 = "Ekstra ayırıcı 1", GX2 = "Ekstra ayırıcı 2", GX3 = "Ekstra ayırıcı 3";
        const string X_TIP = "Saat ve günler sadece 'Saat_ve_günler' türünde kullanılır.";
        [Parameter("Göster", DefaultValue = false, Group = GX1)] public bool X1On { get; set; }
        [Parameter("Yazı", DefaultValue = "", Group = GX1)] public string X1Txt { get; set; }
        [Parameter("Tür", DefaultValue = Tur.Saat_ve_günler, Group = GX1, Description = X_TIP)] public Tur X1Md { get; set; }
        [Parameter("Saat", DefaultValue = 9, MinValue = 0, MaxValue = 23, Group = GX1)] public int X1H { get; set; }
        [Parameter("Dakika", DefaultValue = 30, MinValue = 0, MaxValue = 59, Group = GX1)] public int X1M { get; set; }
        [Parameter("Günler", DefaultValue = "Pzt,Sal,Çar,Per,Cum", Group = GX1, Description = "Virgülle ayır: Paz,Pzt,Sal,Çar,Per,Cum,Cmt")] public string X1Days { get; set; }
        [Parameter("Renk", DefaultValue = "#8C7A5B", Group = GX1)] public Color X1Col { get; set; }
        [Parameter("Stil", DefaultValue = Cizgi.Kesikli, Group = GX1)] public Cizgi X1Sty { get; set; }
        [Parameter("Kalınlık", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = GX1)] public int X1W { get; set; }
        [Parameter("En fazla bu zaman diliminde", DefaultValue = "Hour", Group = GX1)] public TimeFrame X1Tf { get; set; }

        [Parameter("Göster", DefaultValue = false, Group = GX2)] public bool X2On { get; set; }
        [Parameter("Yazı", DefaultValue = "", Group = GX2)] public string X2Txt { get; set; }
        [Parameter("Tür", DefaultValue = Tur.Aylık, Group = GX2, Description = X_TIP)] public Tur X2Md { get; set; }
        [Parameter("Saat", DefaultValue = 9, MinValue = 0, MaxValue = 23, Group = GX2)] public int X2H { get; set; }
        [Parameter("Dakika", DefaultValue = 30, MinValue = 0, MaxValue = 59, Group = GX2)] public int X2M { get; set; }
        [Parameter("Günler", DefaultValue = "Pzt,Sal,Çar,Per,Cum", Group = GX2, Description = "Virgülle ayır: Paz,Pzt,Sal,Çar,Per,Cum,Cmt")] public string X2Days { get; set; }
        [Parameter("Renk", DefaultValue = "#8C7A5B", Group = GX2)] public Color X2Col { get; set; }
        [Parameter("Stil", DefaultValue = Cizgi.Kesikli, Group = GX2)] public Cizgi X2Sty { get; set; }
        [Parameter("Kalınlık", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = GX2)] public int X2W { get; set; }
        [Parameter("En fazla bu zaman diliminde", DefaultValue = "Hour", Group = GX2)] public TimeFrame X2Tf { get; set; }

        [Parameter("Göster", DefaultValue = false, Group = GX3)] public bool X3On { get; set; }
        [Parameter("Yazı", DefaultValue = "", Group = GX3)] public string X3Txt { get; set; }
        [Parameter("Tür", DefaultValue = Tur.Yıllık, Group = GX3, Description = X_TIP)] public Tur X3Md { get; set; }
        [Parameter("Saat", DefaultValue = 9, MinValue = 0, MaxValue = 23, Group = GX3)] public int X3H { get; set; }
        [Parameter("Dakika", DefaultValue = 30, MinValue = 0, MaxValue = 59, Group = GX3)] public int X3M { get; set; }
        [Parameter("Günler", DefaultValue = "Pzt,Sal,Çar,Per,Cum", Group = GX3, Description = "Virgülle ayır: Paz,Pzt,Sal,Çar,Per,Cum,Cmt")] public string X3Days { get; set; }
        [Parameter("Renk", DefaultValue = "#8C7A5B", Group = GX3)] public Color X3Col { get; set; }
        [Parameter("Stil", DefaultValue = Cizgi.Kesikli, Group = GX3)] public Cizgi X3Sty { get; set; }
        [Parameter("Kalınlık", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = GX3)] public int X3W { get; set; }
        [Parameter("En fazla bu zaman diliminde", DefaultValue = "Hour", Group = GX3)] public TimeFrame X3Tf { get; set; }

        // ───────────────────────── Önceki gün / hafta / ay ─────────────────────────
        const string G_PD = "Önceki gün / hafta / ay seviyeleri";
        [Parameter("PDH/PDL", DefaultValue = true, Group = G_PD)] public bool PdOn { get; set; }
        [Parameter("PD renk", DefaultValue = "#2E4A8C", Group = G_PD)] public Color PdCol { get; set; }
        [Parameter("PD stil", DefaultValue = Cizgi.Kesikli, Group = G_PD)] public Cizgi PdSty { get; set; }
        [Parameter("PD kalınlık", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = G_PD)] public int PdW { get; set; }
        [Parameter("PD son kaç gün", DefaultValue = 5, MinValue = 1, MaxValue = 50, Group = G_PD)] public int PdN { get; set; }
        [Parameter("PD en fazla", DefaultValue = "Hour4", Group = G_PD)] public TimeFrame PdTf { get; set; }
        [Parameter("PWH/PWL", DefaultValue = true, Group = G_PD)] public bool PwOn { get; set; }
        [Parameter("PW renk", DefaultValue = "#5B2A4E", Group = G_PD)] public Color PwCol { get; set; }
        [Parameter("PW stil", DefaultValue = Cizgi.Kesikli, Group = G_PD)] public Cizgi PwSty { get; set; }
        [Parameter("PW kalınlık", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = G_PD)] public int PwW { get; set; }
        [Parameter("PW son kaç hafta", DefaultValue = 4, MinValue = 1, MaxValue = 50, Group = G_PD)] public int PwN { get; set; }
        [Parameter("PW en fazla", DefaultValue = "Daily", Group = G_PD)] public TimeFrame PwTf { get; set; }
        [Parameter("PMH/PML", DefaultValue = true, Group = G_PD)] public bool PmOn { get; set; }
        [Parameter("PM renk", DefaultValue = "#2E0602", Group = G_PD)] public Color PmCol { get; set; }
        [Parameter("PM stil", DefaultValue = Cizgi.Kesikli, Group = G_PD)] public Cizgi PmSty { get; set; }
        [Parameter("PM kalınlık", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = G_PD)] public int PmW { get; set; }
        [Parameter("PM son kaç ay", DefaultValue = 3, MinValue = 1, MaxValue = 50, Group = G_PD)] public int PmN { get; set; }
        [Parameter("PM en fazla", DefaultValue = "Weekly", Group = G_PD)] public TimeFrame PmTf { get; set; }

        // ───────────────────────── Açılış boşlukları ─────────────────────────
        const string G_GAP = "Açılış boşlukları (NWOG / NDOG)";
        [Parameter("Orta çizgi (CE)", DefaultValue = true, Group = G_GAP)] public bool GapCe { get; set; }
        [Parameter("NWOG", DefaultValue = true, Group = G_GAP)] public bool NwOn { get; set; }
        [Parameter("NWOG dolgu", DefaultValue = "#806B6560", Group = G_GAP)] public Color NwFill { get; set; }
        [Parameter("NWOG çizgi", DefaultValue = "#6B6560", Group = G_GAP)] public Color NwLine { get; set; }
        [Parameter("NWOG son kaç tane", DefaultValue = 5, MinValue = 1, MaxValue = 20, Group = G_GAP)] public int NwN { get; set; }
        [Parameter("NWOG en fazla", DefaultValue = "Hour4", Group = G_GAP)] public TimeFrame NwTf { get; set; }
        [Parameter("NDOG", DefaultValue = true, Group = G_GAP)] public bool NdOn { get; set; }
        [Parameter("NDOG dolgu", DefaultValue = "#406B6560", Group = G_GAP)] public Color NdFill { get; set; }
        [Parameter("NDOG çizgi", DefaultValue = "#6B6560", Group = G_GAP)] public Color NdLine { get; set; }
        [Parameter("NDOG son kaç tane", DefaultValue = 3, MinValue = 1, MaxValue = 20, Group = G_GAP)] public int NdN { get; set; }
        [Parameter("NDOG en fazla", DefaultValue = "Hour", Group = G_GAP)] public TimeFrame NdTf { get; set; }

        // ───────────────────────── Midnight Open ─────────────────────────
        const string G_MO = "Midnight Open";
        [Parameter("Midnight Open", DefaultValue = true, Group = G_MO, Description = "00:00 NY mumunun açılış fiyatı. Çizgi 17:00 NY'ye kadar uzar.")] public bool MoOn { get; set; }
        [Parameter("Renk", DefaultValue = "#9C6B12", Group = G_MO)] public Color MoCol { get; set; }
        [Parameter("Stil", DefaultValue = Cizgi.Düz, Group = G_MO)] public Cizgi MoSty { get; set; }
        [Parameter("Kalınlık", DefaultValue = 1, MinValue = 1, MaxValue = 4, Group = G_MO)] public int MoW { get; set; }
        [Parameter("Son kaç gün", DefaultValue = 5, MinValue = 1, MaxValue = 50, Group = G_MO)] public int MoN { get; set; }
        [Parameter("En fazla", DefaultValue = "Hour", Group = G_MO)] public TimeFrame MoTf { get; set; }

        // ───────────────────────── FVG ─────────────────────────
        const string G_FVG = "FVG";
        const string EXT_TIP = "FVG en fazla kaç gün uzar. 0 = sınırsız.";
        [Parameter("FVG'leri göster", DefaultValue = true, Group = G_FVG)] public bool FvgOn { get; set; }
        [Parameter("Orta çizgi (CE)", DefaultValue = true, Group = G_FVG)] public bool FvgCe { get; set; }
        [Parameter("Kesinleşme zamanını yaz", DefaultValue = true, Group = G_FVG, Description = "FVG'nin 3. mumunun kapandığı an (NY saati).")] public bool FvgTm { get; set; }
        [Parameter("1m", DefaultValue = false, Group = G_FVG)] public bool Fv1On { get; set; }
        [Parameter("1m renk", DefaultValue = "#6B6560", Group = G_FVG)] public Color Fv1Col { get; set; }
        [Parameter("1m maks. uzatma (gün)", DefaultValue = 1, MinValue = 0, Group = G_FVG, Description = EXT_TIP)] public int Fv1Ext { get; set; }
        [Parameter("5m", DefaultValue = false, Group = G_FVG)] public bool Fv2On { get; set; }
        [Parameter("5m renk", DefaultValue = "#1E7F78", Group = G_FVG)] public Color Fv2Col { get; set; }
        [Parameter("5m maks. uzatma (gün)", DefaultValue = 3, MinValue = 0, Group = G_FVG, Description = EXT_TIP)] public int Fv2Ext { get; set; }
        [Parameter("15m", DefaultValue = false, Group = G_FVG)] public bool Fv3On { get; set; }
        [Parameter("15m renk", DefaultValue = "#9C6B12", Group = G_FVG)] public Color Fv3Col { get; set; }
        [Parameter("15m maks. uzatma (gün)", DefaultValue = 5, MinValue = 0, Group = G_FVG, Description = EXT_TIP)] public int Fv3Ext { get; set; }
        [Parameter("30m", DefaultValue = false, Group = G_FVG)] public bool Fv4On { get; set; }
        [Parameter("30m renk", DefaultValue = "#9C6B12", Group = G_FVG)] public Color Fv4Col { get; set; }
        [Parameter("30m maks. uzatma (gün)", DefaultValue = 7, MinValue = 0, Group = G_FVG, Description = EXT_TIP)] public int Fv4Ext { get; set; }
        [Parameter("1h", DefaultValue = true, Group = G_FVG)] public bool Fv5On { get; set; }
        [Parameter("1h renk", DefaultValue = "#9B1D30", Group = G_FVG)] public Color Fv5Col { get; set; }
        [Parameter("1h maks. uzatma (gün)", DefaultValue = 14, MinValue = 0, Group = G_FVG, Description = EXT_TIP)] public int Fv5Ext { get; set; }
        [Parameter("4h", DefaultValue = true, Group = G_FVG)] public bool Fv6On { get; set; }
        [Parameter("4h renk", DefaultValue = "#4F6F2A", Group = G_FVG)] public Color Fv6Col { get; set; }
        [Parameter("4h maks. uzatma (gün)", DefaultValue = 30, MinValue = 0, Group = G_FVG, Description = EXT_TIP)] public int Fv6Ext { get; set; }
        [Parameter("8h", DefaultValue = false, Group = G_FVG)] public bool Fv7On { get; set; }
        [Parameter("8h renk", DefaultValue = "#4F6F2A", Group = G_FVG)] public Color Fv7Col { get; set; }
        [Parameter("8h maks. uzatma (gün)", DefaultValue = 61, MinValue = 0, Group = G_FVG, Description = EXT_TIP)] public int Fv7Ext { get; set; }
        [Parameter("1D", DefaultValue = true, Group = G_FVG)] public bool Fv8On { get; set; }
        [Parameter("1D renk", DefaultValue = "#2E4A8C", Group = G_FVG)] public Color Fv8Col { get; set; }
        [Parameter("1D maks. uzatma (gün)", DefaultValue = 182, MinValue = 0, Group = G_FVG, Description = EXT_TIP)] public int Fv8Ext { get; set; }
        [Parameter("1W", DefaultValue = true, Group = G_FVG)] public bool Fv9On { get; set; }
        [Parameter("1W renk", DefaultValue = "#5B2A4E", Group = G_FVG)] public Color Fv9Col { get; set; }
        [Parameter("1W maks. uzatma (gün)", DefaultValue = 365, MinValue = 0, Group = G_FVG, Description = EXT_TIP)] public int Fv9Ext { get; set; }
        [Parameter("1M", DefaultValue = false, Group = G_FVG)] public bool Fv10On { get; set; }
        [Parameter("1M renk", DefaultValue = "#2E0602", Group = G_FVG)] public Color Fv10Col { get; set; }
        [Parameter("1M maks. uzatma (gün)", DefaultValue = 1461, MinValue = 0, Group = G_FVG, Description = EXT_TIP)] public int Fv10Ext { get; set; }

        // ───────────────────────── Watermark ─────────────────────────
        const string G_WMT = "Watermark (üst)", G_WMB = "Watermark (alt)";
        [Parameter("Göster", DefaultValue = false, Group = G_WMT)] public bool WmtOn { get; set; }
        [Parameter("Konum", DefaultValue = Konum.Orta, Group = G_WMT)] public Konum WmtPos { get; set; }
        [Parameter("Boyut", DefaultValue = WmBoyu.Normal, Group = G_WMT)] public WmBoyu WmtSize { get; set; }
        [Parameter("Renk", DefaultValue = "#4D2E0602", Group = G_WMT)] public Color WmtCol { get; set; }
        [Parameter("Yazı tipi", DefaultValue = YaziTipi.Normal, Group = G_WMT)] public YaziTipi WmtFont { get; set; }
        [Parameter("Sembol", DefaultValue = true, Group = G_WMT)] public bool WmtSym { get; set; }
        [Parameter("Broker", DefaultValue = true, Group = G_WMT)] public bool WmtExch { get; set; }
        [Parameter("Zaman dilimi", DefaultValue = true, Group = G_WMT)] public bool WmtTf { get; set; }
        [Parameter("Ek metin", DefaultValue = "", Group = G_WMT)] public string WmtTxt { get; set; }

        [Parameter("Göster", DefaultValue = true, Group = G_WMB)] public bool WmbOn { get; set; }
        [Parameter("Konum", DefaultValue = Konum.Orta, Group = G_WMB)] public Konum WmbPos { get; set; }
        [Parameter("Boyut", DefaultValue = WmBoyu.Normal, Group = G_WMB)] public WmBoyu WmbSize { get; set; }
        [Parameter("Renk", DefaultValue = "#4D2E0602", Group = G_WMB)] public Color WmbCol { get; set; }
        [Parameter("Yazı tipi", DefaultValue = YaziTipi.Normal, Group = G_WMB)] public YaziTipi WmbFont { get; set; }
        [Parameter("Sembol", DefaultValue = true, Group = G_WMB)] public bool WmbSym { get; set; }
        [Parameter("Broker", DefaultValue = true, Group = G_WMB)] public bool WmbExch { get; set; }
        [Parameter("Zaman dilimi", DefaultValue = true, Group = G_WMB)] public bool WmbTf { get; set; }
        [Parameter("Ek metin", DefaultValue = "", Group = G_WMB)] public string WmbTxt { get; set; }
        [Parameter("Grafiğin tarihi (alt satır)", DefaultValue = true, Group = G_WMB, Description = "Son mumun NY tarihini yazar.")] public bool WmbDate { get; set; }

        // ════════════════════════════════════════════════════════════════════
        // Yardımcılar
        // ════════════════════════════════════════════════════════════════════
        static readonly string[] DAY_NAMES = { "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi" };
        static readonly string[] DAY_SHORT = { "Paz", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt" };
        static readonly string[] MONTH_NAMES = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
        static readonly string[] MONTH_SHORT = { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };
        static readonly CultureInfo INV = CultureInfo.InvariantCulture;
        const string PFX = "ark|";

        bool Clean => View == Gorunum.Temiz;

        static Color Tr(Color c, int transp) => Color.FromArgb((int)Math.Round(255.0 * (100 - transp) / 100.0), c.R, c.G, c.B);

        static LineStyle Ls(Cizgi c) => c == Cizgi.Kesikli ? LineStyle.Lines : c == Cizgi.Noktalı ? LineStyle.Dots : LineStyle.Solid;

        static double Fs(YaziBoyu s) => s == YaziBoyu.Çok_küçük ? 8 : s == YaziBoyu.Küçük ? 9 : 11;

        static int TfSec(TimeFrame tf)
        {
            if (tf == TimeFrame.Minute) return 60;
            if (tf == TimeFrame.Minute2) return 120;
            if (tf == TimeFrame.Minute3) return 180;
            if (tf == TimeFrame.Minute4) return 240;
            if (tf == TimeFrame.Minute5) return 300;
            if (tf == TimeFrame.Minute6) return 360;
            if (tf == TimeFrame.Minute7) return 420;
            if (tf == TimeFrame.Minute8) return 480;
            if (tf == TimeFrame.Minute9) return 540;
            if (tf == TimeFrame.Minute10) return 600;
            if (tf == TimeFrame.Minute15) return 900;
            if (tf == TimeFrame.Minute20) return 1200;
            if (tf == TimeFrame.Minute30) return 1800;
            if (tf == TimeFrame.Minute45) return 2700;
            if (tf == TimeFrame.Hour) return 3600;
            if (tf == TimeFrame.Hour2) return 7200;
            if (tf == TimeFrame.Hour3) return 10800;
            if (tf == TimeFrame.Hour4) return 14400;
            if (tf == TimeFrame.Hour6) return 21600;
            if (tf == TimeFrame.Hour8) return 28800;
            if (tf == TimeFrame.Hour12) return 43200;
            if (tf == TimeFrame.Daily) return 86400;
            if (tf == TimeFrame.Day2) return 172800;
            if (tf == TimeFrame.Day3) return 259200;
            if (tf == TimeFrame.Weekly) return 604800;
            if (tf == TimeFrame.Monthly) return 2629800;
            return 0; // tick, renko vb.: zaman dilimine bağlı modüller kapalı kalır
        }

        static string TfLabel(int s) =>
            s < 3600 ? (s / 60) + "m" : s < 86400 ? (s / 3600) + "h" : s < 604800 ? (s / 86400) + "D" : s < 2419200 ? (s / 604800) + "W" : Math.Round(s / 2629800.0) + "M";

        static int Tod(DateTime t) => t.Hour * 60 + t.Minute;

        static string Hm(DateTime t) => t.ToString("HH:mm", INV);
        static string Dmy(DateTime t) => t.ToString("dd.MM.yyyy", INV);

        // Günlük mum öğleden sonra açılıyorsa (forex 17:00 NY) işlem günü ertesi takvim günüdür
        static DateTime TradingDay(DateTime open) => open.Hour >= 12 ? open.Date.AddDays(1) : open.Date;
        static DateTime MondayOf(DateTime td) => td.AddDays(-(((int)td.DayOfWeek + 6) % 7));
        static string DayText(DateTime td) => DAY_NAMES[(int)td.DayOfWeek] + " - " + Dmy(td);
        static string WeekText(DateTime td) => Dmy(MondayOf(td)) + " - " + Dmy(MondayOf(td).AddDays(6));
        static string WithCustom(string auto, string custom) => string.IsNullOrEmpty(custom) ? auto : auto + " - " + custom;

        // Süre biçimi: 2g 04:10 / 01:02:03 / 04:05
        static string FmtLeft(TimeSpan ts)
        {
            int s = Math.Max(0, (int)ts.TotalSeconds);
            int d = s / 86400, h = (s % 86400) / 3600, m = (s % 3600) / 60, sec = s % 60;
            return d > 0 ? string.Format(INV, "{0}g {1:00}:{2:00}", d, h, m) : h > 0 ? string.Format(INV, "{0:00}:{1:00}:{2:00}", h, m, sec) : string.Format(INV, "{0:00}:{1:00}", m, sec);
        }

        static bool ParseSess(string s, out int a, out int b)
        {
            a = b = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var p = s.Replace(" ", "").Split('-');
            if (p.Length != 2) return false;
            return ParseHm(p[0], out a) && ParseHm(p[1], out b) && a != b;
        }

        static bool ParseHm(string s, out int v)
        {
            v = 0;
            s = s.Replace(":", "");
            if (s.Length != 4 || !int.TryParse(s, NumberStyles.None, INV, out int n)) return false;
            int h = n / 100, m = n % 100;
            if (h > 24 || m > 59) return false;
            v = (h * 60 + m) % 1440;
            return true;
        }

        static bool InSess(int tod, int a, int b) => a < b ? tod >= a && tod < b : tod >= a || tod < b;

        static bool[] ParseDays(string s)
        {
            var r = new bool[7];
            foreach (var part in (s ?? "").Split(','))
            {
                int k = Array.IndexOf(DAY_SHORT, part.Trim());
                if (k >= 0) r[k] = true;
            }
            return r;
        }

        // Bir zamanın hangi HTF mumuna düştüğü (açılışı t'den küçük/eşit son mum)
        static int IdxAt(Bars hb, DateTime t)
        {
            int lo = 0, hi = hb.Count - 1, res = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                if (hb.OpenTimes[mid] <= t) { res = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            return res;
        }

        // ════════════════════════════════════════════════════════════════════
        // Çizim katmanı: aynı isimli nesne güncellenir, değişmeyen nesneye dokunulmaz
        // ════════════════════════════════════════════════════════════════════
        HashSet<string> _now = new HashSet<string>();
        HashSet<string> _prev = new HashSet<string>();
        readonly Dictionary<string, string> _sig = new Dictionary<string, string>();

        bool Need(string name, string sig)
        {
            _now.Add(name);
            if (_sig.TryGetValue(name, out var s) && s == sig) return false;
            _sig[name] = sig;
            return true;
        }

        // border veya fill null ise o kısım çizilmez
        void Box(string key, int x1, double y1, int x2, double y2, Color border, LineStyle bsty, Color fill)
        {
            if (fill != null)
            {
                string n = PFX + key + "#f";
                if (Need(n, $"{x1}|{y1}|{x2}|{y2}|{fill}"))
                {
                    var r = Chart.DrawRectangle(n, x1, y1, x2, y2, fill, 1, LineStyle.Solid);
                    r.IsFilled = true;
                }
            }
            if (border != null)
            {
                string n = PFX + key + "#b";
                if (Need(n, $"{x1}|{y1}|{x2}|{y2}|{border}|{bsty}"))
                {
                    var r = Chart.DrawRectangle(n, x1, y1, x2, y2, border, 1, bsty);
                    r.IsFilled = false;
                }
            }
        }

        void BoxT(string key, DateTime t1, double y1, DateTime t2, double y2, Color border, LineStyle bsty, Color fill)
        {
            string nf = PFX + key + "#f";
            if (Need(nf, $"{t1.Ticks}|{y1}|{t2.Ticks}|{y2}|{fill}"))
            {
                var r = Chart.DrawRectangle(nf, t1, y1, t2, y2, fill, 1, LineStyle.Solid);
                r.IsFilled = true;
            }
            string nb = PFX + key + "#b";
            if (Need(nb, $"{t1.Ticks}|{y1}|{t2.Ticks}|{y2}|{border}|{bsty}"))
            {
                var r = Chart.DrawRectangle(nb, t1, y1, t2, y2, border, 1, bsty);
                r.IsFilled = false;
            }
        }

        void Ln(string key, int x1, double y1, int x2, double y2, Color c, int w, LineStyle s)
        {
            string n = PFX + key;
            if (Need(n, $"{x1}|{y1}|{x2}|{y2}|{c}|{w}|{s}"))
                Chart.DrawTrendLine(n, x1, y1, x2, y2, c, w, s);
        }

        void LnT(string key, DateTime t1, double y1, DateTime t2, double y2, Color c, int w, LineStyle s)
        {
            string n = PFX + key;
            if (Need(n, $"{t1.Ticks}|{y1}|{t2.Ticks}|{y2}|{c}|{w}|{s}"))
                Chart.DrawTrendLine(n, t1, y1, t2, y2, c, w, s);
        }

        void VLn(string key, int x, Color c, int w, LineStyle s)
        {
            string n = PFX + key;
            if (Need(n, $"{x}|{c}|{w}|{s}"))
                Chart.DrawVerticalLine(n, x, c, w, s);
        }

        void VLnT(string key, DateTime t, Color c, int w, LineStyle s)
        {
            string n = PFX + key;
            if (Need(n, $"{t.Ticks}|{c}|{w}|{s}"))
                Chart.DrawVerticalLine(n, t, c, w, s);
        }

        // h: Right = yazı noktanın sağında, Left = solunda. v: Top = noktanın üstünde, Bottom = altında
        void Txt(string key, string text, int x, double y, Color c, double size, HorizontalAlignment h, VerticalAlignment v, bool bold = false)
        {
            string n = PFX + key;
            if (Need(n, $"{text}|{x}|{y}|{c}|{size}|{h}|{v}|{bold}"))
            {
                var t = Chart.DrawText(n, text, x, y, c);
                t.FontSize = size;
                t.HorizontalAlignment = h;
                t.VerticalAlignment = v;
                t.IsBold = bold;
            }
        }

        void TxtT(string key, string text, DateTime x, double y, Color c, double size, HorizontalAlignment h, VerticalAlignment v)
        {
            string n = PFX + key;
            if (Need(n, $"{text}|{x.Ticks}|{y}|{c}|{size}|{h}|{v}"))
            {
                var t = Chart.DrawText(n, text, x, y, c);
                t.FontSize = size;
                t.HorizontalAlignment = h;
                t.VerticalAlignment = v;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Modeller
        // ════════════════════════════════════════════════════════════════════
        class Level
        {
            public string Key, Name;
            public double Price;
            public bool IsHigh;
            public int X1, X2, StartIdx, Days, TakenIdx = -1;
            public int? EndTod;
            public Color Col;
            public LineStyle Sty;
            public int W;
            public bool Dead, Faded;
        }

        class Slot
        {
            public bool On, FillOn, BordOn, LvlOn;
            public string Name;
            public int SA, SB;
            public bool SessOk;
            public Color FillCol, BordCol, LvlCol;
            public LineStyle BordSty, LvlSty;
            public int LvlW;
            public bool Active;
            public KzRec Cur;
            public List<KzRec> Recs = new List<KzRec>();
        }

        class KzRec
        {
            public int Start, End, HiIdx, LoIdx;
            public double Hi, Lo;
            public bool Faded;
            public Level HiL, LoL;
        }

        class Sep
        {
            public bool On;
            public string Txt, Mode = "";
            public Color Col;
            public LineStyle Sty;
            public int W, MaxSec, Hh, Mm;
            public bool[] Days;
        }

        class PrevLvl
        {
            public string Name;
            public bool On, Seen;
            public Color Col;
            public LineStyle Sty;
            public int W, Keep, MaxSec;
            public List<Level> Hist = new List<Level>();
            public double Hi = double.NaN, Lo = double.NaN;
            public int HiIdx, LoIdx;
        }

        class GapItem
        {
            public int X1, X2, CeIdx = -1;
            public double Top, Bot;
            public bool Up, Live = true;
        }

        class FvgItem
        {
            public double Top, Bot;
            public bool Up, Live = true;
            public DateTime TStart, Appear, Conf, Right;
            public DateTime? EndT, CeT;
        }

        // ════════════════════════════════════════════════════════════════════
        // Durum
        // ════════════════════════════════════════════════════════════════════
        int _chartSec;
        Bars _d, _w, _m, _smA, _smB;
        string _smtSym = "", _smtName = "";
        bool _smtInv;
        TimeFrame _smtTf;
        readonly Dictionary<int, Bars> _fvgBars = new Dictionary<int, Bars>();
        readonly List<(TimeFrame tf, int sec, string name)> FVG_TFS = new List<(TimeFrame, int, string)>();
        bool _isForex;
        int _lastBuilt = -1;
        bool _dirty;
        DateTime _lastBuildUtc = DateTime.MinValue;
        readonly List<string> _bottomTexts = new List<string>();

        protected override void Initialize()
        {
            _chartSec = TfSec(TimeFrame);

            // Biraz daha geçmiş yükle: önceki gün/hafta seviyeleri ve killzone geçmişi için
            for (int k = 0; k < 10 && Bars.Count < MaxBars; k++)
                if (Bars.LoadMoreHistory() <= 0) break;

            _d = MarketData.GetBars(TimeFrame.Daily);
            _w = MarketData.GetBars(TimeFrame.Weekly);
            _m = MarketData.GetBars(TimeFrame.Monthly);

            _isForex = DetectForex(SymbolName);

            // SMT
            _smtTf = SmtTfSel == Secim.Otomatik ? SmtAuto() : SmtTfManual;
            _smtSym = SmtSymSel == Secim.Elle ? (SmtSymManual ?? "").Trim() : SmtAutoSymbol(SymbolName);
            _smtInv = SmtSymSel == Secim.Elle && SmtInvManual;
            _smtName = _smtSym;
            if (SmtOn && _smtSym != "" && TfSec(_smtTf) > _chartSec && _chartSec > 0 && SymExists(_smtSym))
            {
                _smA = MarketData.GetBars(_smtTf);
                _smB = MarketData.GetBars(_smtTf, _smtSym);
            }

            // FVG zaman dilimleri
            var fv = new (bool on, TimeFrame tf, string name)[]
            {
                (Fv1On, TimeFrame.Minute, "1m"), (Fv2On, TimeFrame.Minute5, "5m"), (Fv3On, TimeFrame.Minute15, "15m"),
                (Fv4On, TimeFrame.Minute30, "30m"), (Fv5On, TimeFrame.Hour, "1h"), (Fv6On, TimeFrame.Hour4, "4h"),
                (Fv7On, TimeFrame.Hour8, "8h"), (Fv8On, TimeFrame.Daily, "1D"), (Fv9On, TimeFrame.Weekly, "1W"), (Fv10On, TimeFrame.Monthly, "1M"),
            };
            for (int k = 0; k < fv.Length; k++)
            {
                int sec = TfSec(fv[k].tf);
                FVG_TFS.Add((fv[k].tf, sec, fv[k].name));
                if (FvgOn && fv[k].on && _chartSec > 0 && sec >= _chartSec)
                    _fvgBars[k] = sec == _chartSec ? Bars : MarketData.GetBars(fv[k].tf);
            }

            Chart.ScrollChanged += _ => PlaceBottomTexts();
            Chart.ZoomChanged += _ => PlaceBottomTexts();
            Chart.SizeChanged += _ => PlaceBottomTexts();
            Chart.ObjectsRemoved += e =>
            {
                foreach (var o in e.ChartObjects)
                    if (o.Name.StartsWith(PFX)) _sig.Remove(o.Name);
            };

            Timer.Start(TimeSpan.FromSeconds(1));
        }

        public override void Calculate(int index)
        {
            if (!IsLastBar) return;
            if (index != _lastBuilt) Rebuild();
            else _dirty = true;
        }

        protected override void OnTimer()
        {
            if (_dirty && (DateTime.UtcNow - _lastBuildUtc).TotalSeconds >= 1)
                Rebuild();
        }

        bool SymExists(string name)
        {
            try { return Symbols.Exists(name); } catch { return false; }
        }

        static readonly HashSet<string> CCY = new HashSet<string> { "USD", "EUR", "GBP", "JPY", "CHF", "AUD", "NZD", "CAD", "SEK", "NOK", "DKK", "SGD", "HKD", "MXN", "ZAR", "TRY", "PLN", "CNH", "HUF", "CZK" };

        static bool DetectForex(string name)
        {
            string n = (name ?? "").ToUpperInvariant();
            if (n.Length < 6) return false;
            string a = n.Substring(0, 3), b = n.Substring(3, 3);
            return CCY.Contains(a) && CCY.Contains(b);
        }

        TimeFrame SmtAuto()
        {
            int s = _chartSec;
            return s <= 60 ? TimeFrame.Minute15 : s <= 300 ? TimeFrame.Hour : s <= 900 ? TimeFrame.Hour4 : s <= 14400 ? TimeFrame.Daily : TimeFrame.Monthly;
        }

        // Karşı sembol: broker'daki adıyla; sonek varsa (EURUSD.r) aynı sonek eklenir
        string SmtAutoSymbol(string name)
        {
            string n = (name ?? "").ToUpperInvariant();
            string suffix = "";
            int dot = n.IndexOfAny(new[] { '.', '_', '-' });
            string core = dot > 0 ? n.Substring(0, dot) : n;
            if (dot > 0) suffix = name.Substring(dot);
            var map = new Dictionary<string, string>
            {
                ["EURUSD"] = "GBPUSD", ["GBPUSD"] = "EURUSD", ["AUDUSD"] = "NZDUSD", ["NZDUSD"] = "AUDUSD",
                ["USDJPY"] = "USDCHF", ["USDCHF"] = "USDJPY", ["USDCAD"] = "USDCHF", ["EURJPY"] = "GBPJPY", ["GBPJPY"] = "EURJPY",
                ["XAUUSD"] = "XAGUSD", ["XAGUSD"] = "XAUUSD",
                ["US100"] = "US500", ["US500"] = "US100", ["US30"] = "US500",
                ["NAS100"] = "SPX500", ["SPX500"] = "NAS100", ["USTEC"] = "US500", ["US2000"] = "US500",
                ["BTCUSD"] = "ETHUSD", ["ETHUSD"] = "BTCUSD",
            };
            if (!map.TryGetValue(core, out var to)) return "";
            if (SymExists(to + suffix)) return to + suffix;
            if (SymExists(to)) return to;
            return "";
        }

        // ════════════════════════════════════════════════════════════════════
        // Ana hesap: her yeni mumda (ve canlı fiyatta en fazla saniyede bir) baştan çizilir
        // ════════════════════════════════════════════════════════════════════
        void Rebuild()
        {
            _lastBuilt = Bars.Count - 1;
            _dirty = false;
            _lastBuildUtc = DateTime.UtcNow;
            _now = new HashSet<string>();
            _bottomTexts.Clear();
            _sepQ.Clear();

            if (_chartSec > 0 && Bars.Count > 5)
            {
                try { Compute(); } catch (Exception ex) { Print("Arrakis: " + ex.Message); }
            }
            Watermark();

            foreach (var n in _prev)
                if (!_now.Contains(n))
                {
                    Chart.RemoveObject(n);
                    _sig.Remove(n);
                }
            _prev = _now;
            PlaceBottomTexts();
        }

        void Compute()
        {
            int n = Bars.Count;
            int last = n - 1;
            int start = Math.Max(1, n - MaxBars);
            var T = Bars.OpenTimes;
            var H = Bars.HighPrices;
            var L = Bars.LowPrices;
            var O = Bars.OpenPrices;
            var C = Bars.ClosePrices;
            double tfMin = _chartSec / 60.0;

            // ── Kutular ──
            bool allowed = AllOn && _chartSec <= TfSec(MaxTf);
            bool fxHour = K3Mode == Piyasa.Otomatik ? _isForex : K3Mode == Piyasa.Forex;
            var slots = new[]
            {
                MakeSlot(K1On, K1Name, K1Sess, K1Fill, K1FillCol, K1Bord, K1BordCol, K1BordSty, K1Lvl, K1LvlCol, K1LvlSty, K1LvlW),
                MakeSlot(K2On, K2Name, K2Sess, K2Fill, K2FillCol, K2Bord, K2BordCol, K2BordSty, K2Lvl, K2LvlCol, K2LvlSty, K2LvlW),
                MakeSlot(K3On, K3Name, fxHour ? K3SessF : K3SessI, K3Fill, K3FillCol, K3Bord, K3BordCol, K3BordSty, K3Lvl, K3LvlCol, K3LvlSty, K3LvlW),
                MakeSlot(K4On, K4Name, K4Sess, K4Fill, K4FillCol, K4Bord, K4BordCol, K4BordSty, K4Lvl, K4LvlCol, K4LvlSty, K4LvlW),
            };

            var pending = new List<Level>();
            var allLevels = new List<Level>();

            // ── Önceki gün/hafta/ay ──
            var prevD = new PrevLvl { Name = "PD", On = PdOn, Col = PdCol, Sty = Ls(PdSty), W = PdW, Keep = PdN, MaxSec = TfSec(PdTf) };
            var prevW = new PrevLvl { Name = "PW", On = PwOn, Col = PwCol, Sty = Ls(PwSty), W = PwW, Keep = PwN, MaxSec = TfSec(PwTf) };
            var prevM = new PrevLvl { Name = "PM", On = PmOn, Col = PmCol, Sty = Ls(PmSty), W = PmW, Keep = PmN, MaxSec = TfSec(PmTf) };

            // ── Boşluklar ──
            var nwog = new List<GapItem>();
            var ndog = new List<GapItem>();
            bool nwOk = NwOn && _chartSec <= TfSec(NwTf);
            bool ndOk = NdOn && _chartSec <= TfSec(NdTf);

            // ── Midnight Open ──
            var mos = new List<(int x1, int x2, double p)>();
            bool moLive = false;
            bool moOk = MoOn && _chartSec <= TfSec(MoTf);

            // ── Ayırıcılar ──
            var sepH = new Sep { On = HOn, Txt = HTxt, Col = HCol, Sty = Ls(HSty), W = HW, MaxSec = TfSec(HTf) };
            var sepM = new Sep { On = MOn, Txt = MTxt, Col = MCol, Sty = Ls(MSty), W = MW, MaxSec = TfSec(MTf), Mode = "Saat", Hh = 9, Mm = 30, Days = new[] { false, true, true, true, true, true, false } };
            var sepD = new Sep { On = DOn, Txt = DTxt, Col = DCol, Sty = Ls(DSty), W = DW, MaxSec = TfSec(DTf) };
            var sepW = new Sep { On = WOn, Txt = WTxt, Col = WCol, Sty = Ls(WSty), W = WW, MaxSec = TfSec(WTf) };
            var extras = new[]
            {
                MakeExtra(X1On, X1Txt, X1Md, X1H, X1M, X1Days, X1Col, X1Sty, X1W, X1Tf),
                MakeExtra(X2On, X2Txt, X2Md, X2H, X2M, X2Days, X2Col, X2Sty, X2W, X2Tf),
                MakeExtra(X3On, X3Txt, X3Md, X3H, X3M, X3Days, X3Col, X3Sty, X3W, X3Tf),
            };
            bool wVis = Vis(sepW), dVis = Vis(sepD), hVis = Vis(sepH), mVis = Vis(sepM);
            double sepFs = Fs(SepLbl), lblFs = Fs(LblSize);
            Color sepTxtCol(Sep s) => Tr(s.Col, Clean ? 40 : 0);

            int prevDi = _d.Count > 0 ? IdxAt(_d, T[start - 1]) : -1;
            int prevWi = _w.Count > 0 ? IdxAt(_w, T[start - 1]) : -1;
            int prevMi = _m.Count > 0 ? IdxAt(_m, T[start - 1]) : -1;

            for (int i = start; i < n; i++)
            {
                DateTime t = T[i], tp = T[i - 1];
                int tod = Tod(t), tpod = Tod(tp);
                int di = _d.Count > 0 ? IdxAt(_d, t) : -1;
                int wi = _w.Count > 0 ? IdxAt(_w, t) : -1;
                int mi = _m.Count > 0 ? IdxAt(_m, t) : -1;
                bool newDay = di != prevDi && di >= 0;
                bool newWeek = wi != prevWi && wi >= 0;
                bool newMonth = mi != prevMi && mi >= 0;
                prevDi = di; prevWi = wi; prevMi = mi;
                bool newHour = t.Hour != tp.Hour || t.Date != tp.Date;
                bool newTradingDay = t.Date != tp.Date && t.DayOfWeek != DayOfWeek.Saturday && t.DayOfWeek != DayOfWeek.Sunday;
                DateTime tdNow = di >= 0 ? TradingDay(_d.OpenTimes[di]) : t.Date;

                // Kutular
                for (int s = 0; s < slots.Length; s++)
                    UpdateSlot(slots[s], s, i, tod, newDay, allowed, H[i], L[i], pending, allLevels);

                // Ayırıcılar: çakışmada haftalık > günlük > saatlik
                if (wVis && newWeek)
                {
                    DateTime wtd = wi >= 0 ? TradingDay(_w.OpenTimes[wi]) : tdNow;
                    DrawSep(sepW, "sw" + i, i, H[i], WithCustom(WeekText(wtd), sepW.Txt), sepFs, sepTxtCol(sepW));
                }
                else if (dVis && newDay)
                    DrawSep(sepD, "sd" + i, i, H[i], WithCustom(DayText(tdNow), sepD.Txt), sepFs, sepTxtCol(sepD));
                else if (hVis && newHour)
                {
                    DrawSep(sepH, "sh" + i, i, H[i], sepH.Txt, sepFs, sepTxtCol(sepH));
                    BottomSep(sepH, "bh" + i, t.ToString("HH", INV) + ":00", i, Tr(sepH.Col, 0));
                }
                foreach (var x in extras)
                    if (Vis(x) && Fires(x, t, tod, tfMin, newMonth, mi))
                        DrawSep(x, "sx" + Array.IndexOf(extras, x) + "_" + i, i, H[i], WithCustom(EventText(x, x.Mode == "Saat_ve_günler" ? t.Date : (mi >= 0 ? TradingDay(_m.OpenTimes[mi]) : t.Date)), x.Txt), sepFs, sepTxtCol(x));
                if (mVis && Fires(sepM, t, tod, tfMin, newMonth, mi))
                {
                    DrawSep(sepM, "sm" + i, i, H[i], sepM.Txt, sepFs, sepTxtCol(sepM));
                    BottomSep(sepM, "bm" + i, "09:30", i, Tr(sepM.Col, 0));
                }

                // Önceki gün/hafta/ay
                Track(prevD, newDay, i, H[i], L[i], pending, allLevels);
                Track(prevW, newWeek, i, H[i], L[i], pending, allLevels);
                Track(prevM, newMonth, i, H[i], L[i], pending, allLevels);

                // Boşluklar: hafta başında günlük boşluk haftalıkla aynıdır, sadece NWOG çizilir
                if (newWeek) AddGap(nwog, nwOk, NwN, C[i - 1], O[i], i);
                else if (newDay) AddGap(ndog, ndOk, NdN, C[i - 1], O[i], i);
                GrowGaps(nwog, i, H[i], L[i]);
                GrowGaps(ndog, i, H[i], L[i]);

                // Midnight Open: temasta durmaz, 17:00 NY'de biter
                if (moOk)
                {
                    if (tod == 0 && tpod != 0)
                    {
                        mos.Add((i, i, O[i]));
                        moLive = true;
                        if (mos.Count > MoN) mos.RemoveAt(0);
                    }
                    else if (moLive)
                    {
                        if (tod < 17 * 60) mos[mos.Count - 1] = (mos[mos.Count - 1].x1, i, mos[mos.Count - 1].p);
                        else moLive = false;
                    }
                }

                // Seviye uzatma: fitil değerse o barda durur; killzone seviyeleri ayrıca süre dolunca durur
                for (int k = pending.Count - 1; k >= 0; k--)
                {
                    var lv = pending[k];
                    if (lv.Dead) { pending.RemoveAt(k); continue; }
                    if (newTradingDay && i > lv.StartIdx) lv.Days++;
                    bool touched = lv.IsHigh ? H[i] >= lv.Price : L[i] <= lv.Price;
                    bool expired = lv.EndTod.HasValue && (lv.Days > ExtDays || (lv.Days == ExtDays && tod >= lv.EndTod.Value));
                    if (!expired)
                    {
                        lv.X2 = i;
                        if (touched) lv.TakenIdx = i;
                    }
                    if (touched || expired) pending.RemoveAt(k);
                }
            }

            // ── Çizim: ayırıcılar (her türün en yeni 300 tanesi; çok fazla nesne cTrader'ı yavaşlatır) ──
            foreach (var grp in _sepQ.GroupBy(q => q.s))
            {
                var list = grp.ToList();
                for (int q = Math.Max(0, list.Count - SEP_KEEP); q < list.Count; q++) list[q].draw();
            }
            _sepQ.Clear();

            // ── Çizim: kutular ──
            for (int s = 0; s < slots.Length; s++)
            {
                var sl = slots[s];
                for (int r = 0; r < sl.Recs.Count; r++)
                {
                    var rec = sl.Recs[r];
                    bool f = Clean && rec.Faded;
                    string key = "kz" + s + "_" + rec.Start;
                    Box(key, rec.Start, rec.Hi, rec.End, rec.Lo,
                        sl.BordOn ? Tr(sl.BordCol, f ? 65 : 0) : (Color)null, sl.BordSty,
                        sl.FillOn ? (f ? Tr(sl.FillCol, 94) : sl.FillCol) : (Color)null);
                    Txt(key + "t", SlotTitle(sl.Name, rec.Hi - rec.Lo), rec.Start, rec.Hi, f ? Tr(sl.BordCol, 65) : sl.BordCol, lblFs, HorizontalAlignment.Right, VerticalAlignment.Top);
                }
            }

            // ── Çizim: seviyeler ──
            foreach (var lv in allLevels)
            {
                if (lv.Dead) continue;
                bool taken = lv.TakenIdx >= 0;
                bool fadeTaken = taken && (Taken == Alinan.İkisi || Taken == Alinan.Soluklaştır);
                Color c = fadeTaken ? Tr(lv.Col, 75) : (Clean && lv.Faded) ? Tr(lv.Col, 70) : lv.Col;
                Ln(lv.Key, lv.X1, lv.Price, lv.X2, lv.Price, c, lv.W, lv.Sty);
                // Killzone tepesi: yazı çizginin altına iner, kutu başlığıyla üst üste binmesin
                bool under = lv.IsHigh && lv.Key.StartsWith("kl");
                Txt(lv.Key + "t", lv.Name, lv.X2, lv.Price, c, lblFs, HorizontalAlignment.Left, under ? VerticalAlignment.Bottom : VerticalAlignment.Top);
                if (taken && (Taken == Alinan.İkisi || Taken == Alinan.Çarpı))
                    Txt(lv.Key + "x", "✕", lv.TakenIdx, lv.Price, lv.Col, 9, HorizontalAlignment.Center, VerticalAlignment.Center);
            }

            // ── Çizim: boşluklar ──
            DrawGaps(nwog, "NWOG", NwFill, NwLine, last);
            DrawGaps(ndog, "NDOG", NdFill, NdLine, last);

            // ── Çizim: Midnight Open ──
            foreach (var mo in mos)
            {
                Ln("mo" + mo.x1, mo.x1, mo.p, mo.x2, mo.p, MoCol, MoW, Ls(MoSty));
                Txt("mo" + mo.x1 + "t", "MO", mo.x2, mo.p, MoCol, lblFs, HorizontalAlignment.Left, VerticalAlignment.Top);
            }

            // ── Gelecek ayırıcılar ──
            if (SepFut) FutureSeps(sepH, sepD, sepW, sepM, extras, hVis, dVis, wVis, mVis, sepFs);

            // ── FVG ──
            DrawFvgs(start);

            // ── SMT ──
            DrawSmt(start);
        }

        bool Vis(Sep s) => s.On && _chartSec <= s.MaxSec;

        Sep MakeExtra(bool on, string txt, Tur md, int hh, int mm, string days, Color col, Cizgi sty, int w, TimeFrame tf) =>
            new Sep { On = on, Txt = txt, Mode = md.ToString(), Hh = hh, Mm = mm, Days = ParseDays(days), Col = col, Sty = Ls(sty), W = w, MaxSec = TfSec(tf) };

        Slot MakeSlot(bool on, string name, string sess, bool fo, Color fc, bool bo, Color bc, Cizgi bs, bool lo, Color lc, Cizgi lsty, int lw)
        {
            var s = new Slot { On = on, Name = name, FillOn = fo, FillCol = fc, BordOn = bo, BordCol = bc, BordSty = Ls(bs), LvlOn = lo, LvlCol = lc, LvlSty = Ls(lsty), LvlW = lw };
            s.SessOk = ParseSess(sess, out s.SA, out s.SB);
            return s;
        }

        // Forex'te pip; diğer sembollerde fiyat farkı "pts" olarak yazılır
        string SlotTitle(string name, double rng) =>
            name + " • " + (_isForex ? (rng / Symbol.PipSize).ToString("0.0", INV) + " pips" : rng.ToString("0.0", INV) + " pts");

        // Biten seans silinmez, sadece soluklaşır; seviyeleri uzamaya devam eder
        void UpdateSlot(Slot s, int sIdx, int i, int tod, bool slotDay, bool allowed, double hi, double lo, List<Level> pending, List<Level> all)
        {
            bool inS = s.On && s.SessOk && allowed && InSess(tod, s.SA, s.SB);
            if (slotDay && !s.Active) Fade(s);
            if (inS && !s.Active)
            {
                Fade(s);
                var rec = new KzRec { Start = i, End = i, Hi = hi, Lo = lo, HiIdx = i, LoIdx = i };
                if (s.LvlOn)
                {
                    rec.HiL = new Level { Key = "kl" + sIdx + "_" + i + "h", Name = s.Name + ".H", Price = hi, IsHigh = true, X1 = i, X2 = i, Col = s.LvlCol, Sty = s.LvlSty, W = s.LvlW };
                    rec.LoL = new Level { Key = "kl" + sIdx + "_" + i + "l", Name = s.Name + ".L", Price = lo, IsHigh = false, X1 = i, X2 = i, Col = s.LvlCol, Sty = s.LvlSty, W = s.LvlW };
                    all.Add(rec.HiL);
                    all.Add(rec.LoL);
                }
                s.Cur = rec;
                s.Recs.Add(rec);
            }
            else if (inS)
            {
                var r = s.Cur;
                if (hi > r.Hi) { r.Hi = hi; r.HiIdx = i; }
                if (lo < r.Lo) { r.Lo = lo; r.LoIdx = i; }
                r.End = i;
                if (r.HiL != null)
                {
                    r.HiL.Price = r.Hi; r.HiL.X1 = r.HiIdx; r.HiL.X2 = i;
                    r.LoL.Price = r.Lo; r.LoL.X1 = r.LoIdx; r.LoL.X2 = i;
                }
            }
            else if (s.Active && s.Cur != null && s.Cur.HiL != null)
            {
                // Seans bitti: seviyeler artık temas/süre kontrolüyle uzar
                s.Cur.HiL.StartIdx = i; s.Cur.HiL.EndTod = tod;
                s.Cur.LoL.StartIdx = i; s.Cur.LoL.EndTod = tod;
                pending.Add(s.Cur.HiL);
                pending.Add(s.Cur.LoL);
            }
            s.Active = inS;
        }

        void Fade(Slot s)
        {
            if (s.Cur == null) return;
            s.Cur.Faded = true;
            if (s.Cur.HiL != null) { s.Cur.HiL.Faded = true; s.Cur.LoL.Faded = true; }
        }

        // Dönem değişince biten dönemin high/low'u, oluştuğu bardan başlayan seviyeye dönüşür
        void Track(PrevLvl p, bool isNew, int i, double hi, double lo, List<Level> pending, List<Level> all)
        {
            if (isNew)
            {
                if (p.Seen && p.On && _chartSec <= p.MaxSec)
                {
                    foreach (var (price, x, isHigh) in new[] { (p.Hi, p.HiIdx, true), (p.Lo, p.LoIdx, false) })
                    {
                        var lv = new Level { Key = p.Name + i + (isHigh ? "h" : "l"), Name = p.Name + (isHigh ? "H" : "L"), Price = price, IsHigh = isHigh, X1 = x, X2 = i, StartIdx = i, Col = p.Col, Sty = p.Sty, W = p.W };
                        p.Hist.Add(lv);
                        pending.Add(lv);
                        all.Add(lv);
                    }
                    while (p.Hist.Count > p.Keep * 2)
                    {
                        p.Hist[0].Dead = true;
                        p.Hist.RemoveAt(0);
                    }
                }
                p.Seen = true;
                p.Hi = hi; p.Lo = lo; p.HiIdx = i; p.LoIdx = i;
            }
            else
            {
                if (double.IsNaN(p.Hi) || hi > p.Hi) { p.Hi = hi; p.HiIdx = i; }
                if (double.IsNaN(p.Lo) || lo < p.Lo) { p.Lo = lo; p.LoIdx = i; }
            }
        }

        // Önceki mumun kapanışı ile yeni mumun açılışı arasındaki boşluk, dolana kadar sağa uzanan kutu olur
        void AddGap(List<GapItem> g, bool ok, int keep, double pc, double open, int i)
        {
            double top = Math.Max(pc, open), bot = Math.Min(pc, open);
            if (!ok || !(top > bot)) return;
            g.Add(new GapItem { X1 = i, X2 = i, Top = top, Bot = bot, Up = open > pc });
            if (g.Count > keep) g.RemoveAt(0);
        }

        // Yukarı boşluk fiyat alt kenara inince, aşağı boşluk üst kenara çıkınca dolmuş sayılır; orta çizgiye değince CE soluklaşır
        void GrowGaps(List<GapItem> g, int i, double hi, double lo)
        {
            foreach (var it in g)
            {
                if (!it.Live) continue;
                it.X2 = i;
                double mid = (it.Top + it.Bot) / 2;
                if (it.CeIdx < 0 && (it.Up ? lo <= mid : hi >= mid)) it.CeIdx = i - 2;
                if (it.Up ? lo <= it.Bot : hi >= it.Top) it.Live = false;
            }
        }

        void DrawGaps(List<GapItem> g, string name, Color fill, Color line, int last)
        {
            foreach (var it in g)
            {
                string key = name + it.X1;
                double mid = (it.Top + it.Bot) / 2;
                Box(key, it.X1, it.Top, it.X2, it.Bot, line, LineStyle.Solid, fill);
                if (GapCe) Ln(key + "c", it.X1, mid, it.X2, mid, it.CeIdx >= 0 ? Tr(line, 75) : line, 1, LineStyle.Lines);
                if (it.Live)
                    Txt(key + "t", name, last + 2, mid, line, 11, HorizontalAlignment.Right, VerticalAlignment.Center);
                else
                    Txt(key + "t", name, it.CeIdx >= 0 ? it.CeIdx : it.X2, mid, line, 11, HorizontalAlignment.Left, VerticalAlignment.Center);
            }
        }

        // ── Ayırıcı yardımcıları ──
        const int SEP_KEEP = 600;   // ayırıcı başına kayıt (çizgi + alt yazı) sınırı
        readonly List<(Sep s, Action draw)> _sepQ = new List<(Sep s, Action draw)>();

        void DrawSep(Sep s, string key, int i, double high, string txt, double fs, Color txtCol) =>
            _sepQ.Add((s, () => DrawSepNow(s, key, i, high, txt, fs, txtCol)));

        void BottomSep(Sep s, string key, string text, int x, Color c) =>
            _sepQ.Add((s, () => BottomText(key, text, x, c)));

        void DrawSepNow(Sep s, string key, int i, double high, string txt, double fs, Color txtCol)
        {
            VLn(key, i, s.Col, s.W, s.Sty);
            if (!string.IsNullOrEmpty(txt))
                Txt(key + "t", txt, i, high + Symbol.TickSize * 2, txtCol, fs, HorizontalAlignment.Right, VerticalAlignment.Top);
        }

        bool Fires(Sep s, DateTime t, int tod, double tfMin, bool newMonth, int mi)
        {
            int target = s.Hh * 60 + s.Mm;
            switch (s.Mode)
            {
                case "Aylık": return newMonth;
                case "Üç_aylık": return newMonth && mi >= 0 && (TradingDay(_m.OpenTimes[mi]).Month - 1) % 3 == 0;
                case "Yıllık": return newMonth && mi >= 0 && TradingDay(_m.OpenTimes[mi]).Month == 1;
                default: return s.Days[(int)t.DayOfWeek] && tod <= target && tod + tfMin > target;
            }
        }

        string EventText(Sep s, DateTime d)
        {
            switch (s.Mode)
            {
                case "Aylık": return MONTH_NAMES[d.Month - 1] + " " + d.Year;
                case "Üç_aylık": return "Q" + ((d.Month - 1) / 3 + 1) + " " + d.Year;
                case "Yıllık": return d.Year.ToString(INV);
                case "Saat": return string.Format(INV, "{0:00}:{1:00}", s.Hh, s.Mm);
                default: return DAY_NAMES[(int)d.DayOfWeek] + " " + string.Format(INV, "{0:00}:{1:00}", s.Hh, s.Mm) + " - " + Dmy(d);
            }
        }

        // Saat başı yazıları grafiğin en altına sabitlenir; kaydırınca yeniden yerleşir
        void BottomText(string key, string text, int x, Color c)
        {
            string n = PFX + key;
            _bottomTexts.Add(n);
            if (Need(n, $"{text}|{x}|{c}"))
            {
                var t = Chart.DrawText(n, text, x, BottomY(), c);
                t.FontSize = 9;
                t.HorizontalAlignment = HorizontalAlignment.Center;
                t.VerticalAlignment = VerticalAlignment.Top;
            }
        }

        double BottomY()
        {
            double top = Chart.TopY, bot = Chart.BottomY;
            return bot + (top - bot) * 0.005;
        }

        void PlaceBottomTexts()
        {
            if (_bottomTexts.Count == 0) return;
            double y = BottomY();
            foreach (var n in _bottomTexts)
                if (Chart.FindObject(n) is ChartText t) t.Y = y;
        }

        // Sıradaki mumlar: yazıları zamana göre sıralanıp mumların üzerinde yukarı doğru dizilir
        void FutureSeps(Sep sepH, Sep sepD, Sep sepW, Sep sepM, Sep[] extras, bool hVis, bool dVis, bool wVis, bool mVis, double fs)
        {
            int n = Bars.Count, last = n - 1;
            DateTime lastT = Bars.OpenTimes[last];
            DateTime barEnd = lastT.AddSeconds(_chartSec);
            var items = new List<(string key, Sep s, DateTime t, string txt)>();

            if (_w.Count > 0 && _d.Count > 0)
            {
                DateTime lastW = _w.OpenTimes[_w.Count - 1];
                DateTime nextWeekT = lastW.AddDays(7);
                DateTime nextWeekTd = TradingDay(nextWeekT);
                DateTime lastD = _d.OpenTimes[_d.Count - 1];
                DateTime nextDayT = lastD.AddDays(1);
                while (TradingDay(nextDayT).DayOfWeek == DayOfWeek.Saturday || TradingDay(nextDayT).DayOfWeek == DayOfWeek.Sunday)
                    nextDayT = nextDayT.AddDays(1);
                DateTime nextHourT = new DateTime(lastT.Year, lastT.Month, lastT.Day, lastT.Hour, 0, 0).AddHours(1);
                if (wVis) items.Add(("fw", sepW, nextWeekT, WithCustom(WeekText(nextWeekTd), sepW.Txt)));
                if (dVis && !(wVis && nextDayT == nextWeekT)) items.Add(("fd", sepD, nextDayT, WithCustom(DayText(TradingDay(nextDayT)), sepD.Txt)));
                if (hVis && !(wVis && nextHourT == nextWeekT) && !(dVis && nextHourT == nextDayT)) items.Add(("fh", sepH, nextHourT, WithCustom(Hm(nextHourT), sepH.Txt)));
            }
            var all = new List<(Sep s, bool vis, string key)> { (sepM, mVis, "fm") };
            for (int k = 0; k < extras.Length; k++) all.Add((extras[k], Vis(extras[k]), "fx" + k));
            foreach (var (s, vis, key) in all)
            {
                if (!vis) continue;
                var ev = NextEvent(s, lastT, barEnd);
                if (ev.HasValue) items.Add((key, s, ev.Value.t, WithCustom(EventText(s, ev.Value.day), s.Txt)));
            }

            // Yazılar son 100 mumun tepesinin üzerinde, zamana göre sıralı basamaklar halinde
            int k0 = Math.Max(0, n - 100);
            double hi = double.MinValue, lo = double.MaxValue;
            for (int i = k0; i < n; i++) { hi = Math.Max(hi, Bars.HighPrices[i]); lo = Math.Min(lo, Bars.LowPrices[i]); }
            double step = (hi - lo) * 0.03;
            int row = 0;
            foreach (var it in items.OrderBy(x => x.t))
            {
                VLnT(it.key, it.t, it.s.Col, it.s.W, it.s.Sty);
                TxtT(it.key + "t", it.txt, it.t, hi + (row + 2) * step, Tr(it.s.Col, 0), fs, HorizontalAlignment.Right, VerticalAlignment.Center);
                row++;
            }
        }

        (DateTime t, DateTime day)? NextEvent(Sep s, DateTime lastT, DateTime barEnd)
        {
            if (s.Mode == "Saat" || s.Mode == "Saat_ve_günler")
            {
                for (int k = 0; k <= 7; k++)
                {
                    DateTime d = lastT.Date.AddDays(k);
                    DateTime c = d.AddHours(s.Hh).AddMinutes(s.Mm);
                    if (s.Days[(int)d.DayOfWeek] && c >= barEnd) return (c, d);
                }
                return null;
            }
            if (_d.Count == 0) return null;
            DateTime lastD = _d.OpenTimes[_d.Count - 1];
            DateTime td = TradingDay(lastD);
            int y = td.Year, m = td.Month;
            m = s.Mode == "Aylık" ? m + 1 : s.Mode == "Üç_aylık" ? ((m - 1) / 3) * 3 + 4 : 13;
            if (m > 12) { y++; m -= 12; }
            DateTime first = new DateTime(y, m, 1);
            if (first.DayOfWeek == DayOfWeek.Saturday) first = first.AddDays(2);
            else if (first.DayOfWeek == DayOfWeek.Sunday) first = first.AddDays(1);
            // Günlük mum işlem gününden önceki akşam açılıyorsa (forex) olay o akşamdır
            bool openPrev = lastD.Date != td;
            DateTime startT = (openPrev ? first.AddDays(-1) : first).Add(lastD.TimeOfDay);
            return (startT, first);
        }

        // ════════════════════════════════════════════════════════════════════
        // FVG
        // ════════════════════════════════════════════════════════════════════
        void DrawFvgs(int start)
        {
            if (_fvgBars.Count == 0) return;
            int n = Bars.Count, last = n - 1;
            var T = Bars.OpenTimes;
            DateTime winStart = T[start];
            DateTime lastT = T[last];
            double close = Bars.ClosePrices[last];
            var cols = new[] { Fv1Col, Fv2Col, Fv3Col, Fv4Col, Fv5Col, Fv6Col, Fv7Col, Fv8Col, Fv9Col, Fv10Col };
            var exts = new[] { Fv1Ext, Fv2Ext, Fv3Ext, Fv4Ext, Fv5Ext, Fv6Ext, Fv7Ext, Fv8Ext, Fv9Ext, Fv10Ext };

            foreach (var kv in _fvgBars)
            {
                int k = kv.Key;
                Bars hb = kv.Value;
                var (tf, sec, name) = FVG_TFS[k];
                Color col = cols[k];
                int extD = exts[k];
                double ratio = (double)sec / _chartSec;
                int fill = !Clean || ratio <= 24 ? 85 : ratio <= 96 ? 89 : 92;
                int fillDim = Clean ? Math.Min(fill + 6, 96) : fill;
                string fmt = sec >= 86400 ? "dd.MM.yyyy" : "dd.MM HH:mm";
                string label = name + " FVG";

                DateTime earliest = extD > 0 ? winStart.AddDays(-extD) : DateTime.MinValue;
                int mFrom = Math.Max(3, hb.Count - 400);
                var items = new List<FvgItem>();
                for (int m = mFrom; m < hb.Count; m++)
                {
                    DateTime appear = hb.OpenTimes[m];
                    if (appear < earliest || appear > lastT) continue;
                    double l1 = hb.LowPrices[m - 1], h1 = hb.HighPrices[m - 1], l3 = hb.LowPrices[m - 3], h3 = hb.HighPrices[m - 3];
                    bool up = l1 > h3, dn = h1 < l3;
                    if (!up && !dn) continue;
                    var it = new FvgItem
                    {
                        Up = up,
                        Top = up ? l1 : l3,
                        Bot = up ? h3 : h1,
                        TStart = hb.OpenTimes[m - 3],
                        Appear = appear,
                        Conf = ConfTime(hb, m, sec),
                        EndT = extD > 0 ? appear.AddDays(extD) : (DateTime?)null,
                    };
                    double mid = (it.Top + it.Bot) / 2;

                    // Grafikte yüklü olmayan kısım: HTF mumlarıyla kontrol edilir
                    int j = m;
                    for (; j < hb.Count && it.Live; j++)
                    {
                        DateTime jEnd = j + 1 < hb.Count ? hb.OpenTimes[j + 1] : DateTime.MaxValue;
                        if (jEnd > winStart) break;
                        double hh = hb.HighPrices[j], ll = hb.LowPrices[j];
                        if (it.CeT == null && (up ? ll <= mid : hh >= mid)) it.CeT = hb.OpenTimes[j];
                        if ((up ? ll <= it.Bot : hh >= it.Top) || (it.EndT.HasValue && hb.OpenTimes[j] >= it.EndT.Value))
                        {
                            it.Live = false;
                            it.Right = hb.OpenTimes[j];
                        }
                    }
                    // Grafik mumlarıyla kontrol
                    if (it.Live)
                    {
                        int i0 = appear <= winStart ? start : FirstIdxAtOrAfter(appear, start);
                        if (j < hb.Count && hb.OpenTimes[j] > winStart && appear < winStart) i0 = FirstIdxAtOrAfter(hb.OpenTimes[j], start);
                        for (int i = i0; i < n && it.Live; i++)
                        {
                            double hh = Bars.HighPrices[i], ll = Bars.LowPrices[i];
                            if (it.CeT == null && (up ? ll <= mid : hh >= mid)) it.CeT = T[Math.Max(0, i - 2)];
                            if ((up ? ll <= it.Bot : hh >= it.Top) || (it.EndT.HasValue && T[i] >= it.EndT.Value))
                            {
                                it.Live = false;
                                it.Right = T[i];
                            }
                        }
                    }
                    if (it.Live) it.Right = lastT;
                    items.Add(it);
                }

                // Fiyatın üstündeki ve altındaki en yakın canlı FVG tam güçte kalır
                FvgItem nearA = null, nearB = null;
                double dA = double.MaxValue, dB = double.MaxValue;
                foreach (var it in items.Where(x => x.Live))
                {
                    bool above = it.Bot > close;
                    double d = above ? it.Bot - close : Math.Max(close - it.Top, 0);
                    if (above && d < dA) { dA = d; nearA = it; }
                    else if (!above && d < dB) { dB = d; nearB = it; }
                }

                foreach (var it in items)
                {
                    string key = "fv" + k + "_" + it.TStart.Ticks + (it.Up ? "u" : "d");
                    double mid = (it.Top + it.Bot) / 2;
                    string text = FvgTm ? label + " · " + it.Conf.ToString(fmt, INV) : label;
                    if (it.Live)
                    {
                        bool near = it == nearA || it == nearB;
                        bool dim = Clean && !near;
                        BoxT(key, it.TStart, it.Top, it.Right, it.Bot, Tr(col, dim ? 85 : 70), LineStyle.Solid, Tr(col, near ? fill : fillDim));
                        if (FvgCe) LnT(key + "c", it.TStart, mid, it.Right, mid, Tr(col, dim ? 88 : it.CeT == null ? 0 : 75), 1, LineStyle.Lines);
                        TxtT(key + "t", text, lastT.AddSeconds(2 * _chartSec), mid, col, 11, HorizontalAlignment.Right, VerticalAlignment.Center);
                    }
                    else
                    {
                        BoxT(key, it.TStart, it.Top, it.Right, it.Bot, Tr(col, Clean ? 85 : 70), LineStyle.Solid, Tr(col, Clean ? fillDim : fill));
                        if (FvgCe) LnT(key + "c", it.TStart, mid, it.Right, mid, Tr(col, Clean ? 88 : it.CeT == null ? 0 : 75), 1, LineStyle.Lines);
                        TxtT(key + "t", text, it.CeT ?? it.Right, mid, Clean ? Tr(col, 45) : col, 11, HorizontalAlignment.Left, VerticalAlignment.Center);
                    }
                }
            }
        }

        int FirstIdxAtOrAfter(DateTime t, int start)
        {
            int lo = start, hi = Bars.Count - 1, res = Bars.Count;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                if (Bars.OpenTimes[mid] >= t) { res = mid; hi = mid - 1; }
                else lo = mid + 1;
            }
            return res;
        }

        // FVG'nin 3. mumunun kapanış anı (NY)
        static DateTime ConfTime(Bars hb, int m, int sec)
        {
            DateTime o = hb.OpenTimes[m - 1];
            if (sec < 86400) return o.AddSeconds(sec);
            DateTime td = TradingDay(o);
            if (sec < 604800) return td;
            if (sec < 2419200) return MondayOf(td).AddDays(4);
            return new DateTime(td.Year, td.Month, DateTime.DaysInMonth(td.Year, td.Month));
        }

        // ════════════════════════════════════════════════════════════════════
        // SMT
        // ════════════════════════════════════════════════════════════════════
        const int SN = 100;

        static (double[] h, double[] l, DateTime[] t) Pack(Bars b)
        {
            var h = new double[SN];
            var l = new double[SN];
            var t = new DateTime[SN];
            for (int i = 0; i < SN; i++)
            {
                int k = b.Count - 1 - i;
                h[i] = k >= 0 ? b.HighPrices[k] : double.NaN;
                l[i] = k >= 0 ? b.LowPrices[k] : double.NaN;
                t[i] = k >= 0 ? b.OpenTimes[k] : DateTime.MinValue;
            }
            return (h, l, t);
        }

        // Solunda ve sağında p mumdan daha uç olan swing; eşit tepelerde eskisi sayılır
        static bool IsPivot(double[] a, int i, int p, bool hi)
        {
            if (double.IsNaN(a[i])) return false;
            for (int k = i - p; k <= i + p; k++)
            {
                if (k == i) continue;
                if (k < 0 || k >= a.Length) return false;
                double v = a[k];
                if (double.IsNaN(v) || (hi ? v > a[i] : v < a[i]) || (k < i && v == a[i])) return false;
            }
            return true;
        }

        // Swing iki sembolde birkaç mum kayabilir; aynı zamanın ±p mumundaki en uç mum alınır
        static int SmtIdx(double[] a, int j, int p, bool hi)
        {
            int best = -1;
            for (int k = Math.Max(0, j - p); k <= Math.Min(a.Length - 1, j + p); k++)
            {
                double v = a[k];
                if (!double.IsNaN(v) && (best < 0 || (hi ? v > a[best] : v < a[best]))) best = k;
            }
            return best;
        }

        // Bir sembolün art arda iki swing'i, ana sembolün mum sırasıyla kaydedilir
        void SmtCollect(List<int> keys, double[] src, DateTime[] srcT, DateTime[] t, bool hi)
        {
            int prev = -1;
            for (int i = SmtP + 1; i <= SN - 1 - SmtP; i++)
            {
                if (!IsPivot(src, i, SmtP, hi)) continue;
                int m = Array.IndexOf(t, srcT[i]);
                if (m < 0) continue;
                if (prev >= 0 && !keys.Contains(prev * 1000 + m)) keys.Add(prev * 1000 + m);
                prev = m;
            }
        }

        // Çizginin ucu fitilin üstüne otursun diye HTF mumunun içindeki en uç grafik mumu aranır
        DateTime SmtBarOf(DateTime tFrom, DateTime tTo, bool hi, int start)
        {
            DateTime best = tFrom;
            double val = double.NaN;
            for (int k = Bars.Count - 1; k >= start; k--)
            {
                DateTime tk = Bars.OpenTimes[k];
                if (tk < tFrom) break;
                if (tk < tTo)
                {
                    double v = hi ? Bars.HighPrices[k] : Bars.LowPrices[k];
                    if (double.IsNaN(val) || (hi ? v > val : v < val)) { val = v; best = tk; }
                }
            }
            return best;
        }

        void DrawSmt(int start)
        {
            if (_smA == null || _smB == null || _smB.Count < 10) return;
            var (aH, aL, aT) = Pack(_smA);
            var (bH, bL, bT) = Pack(_smB);
            SmtSide(aH, _smtInv ? bL : bH, aT, bT, true, start);
            SmtSide(aL, _smtInv ? bH : bL, aT, bT, false, start);
        }

        // İki sembolün art arda iki swing'inde biri seviyeyi alıp diğeri almadıysa SMT'dir; high'larda bearish, low'larda bullish
        void SmtSide(double[] a, double[] xa, DateTime[] t, DateTime[] xT, bool hi, int start)
        {
            Color cl = hi ? SmtBear : SmtBull;
            bool cHi = _smtInv ? !hi : hi;
            int found = 0;
            var keys = new List<int>();
            var seen = new List<int>();
            SmtCollect(keys, a, t, t, hi);
            SmtCollect(keys, xa, xT, t, cHi);
            keys.Sort();
            string tfl = TfLabel(TfSec(_smtTf));
            foreach (int key in keys)
            {
                if (found >= SmtMax) break;
                int cB = key / 1000, cA = key % 1000;
                int jA = Array.IndexOf(xT, t[cA]);
                int jB = Array.IndexOf(xT, t[cB]);
                if (jA < 0 || jB < 0) continue;
                int eA = SmtIdx(a, cA, SmtP, hi), eB = SmtIdx(a, cB, SmtP, hi);
                int oA = SmtIdx(xa, jA, SmtP, cHi), oB = SmtIdx(xa, jB, SmtP, cHi);
                if (!(eA > eB && eB >= 1 && oA > oB && oB >= 1) || seen.Contains(eB * 1000 + eA)) continue;
                bool mainTook = hi ? a[eB] > a[eA] : a[eB] < a[eA];
                bool otherTook = cHi ? xa[oB] > xa[oA] : xa[oB] < xa[oA];
                if (mainTook == otherTook) continue;
                seen.Add(eB * 1000 + eA);
                DateTime tA = SmtBarOf(t[eA], t[eA - 1], hi, start);
                DateTime tB = SmtBarOf(t[eB], t[eB - 1], hi, start);
                // İlk bulunan en yenisidir; eskiler soluk kalır
                Color c = Clean && found > 0 ? Tr(cl, 45) : cl;
                string s = Clean ? "SMT " + tfl + " " + _smtName + (otherTook ? " ✓" : " ✗") : "SMT " + tfl + " · " + _smtName + (otherTook ? " aldı" : " almadı");
                string k = "smt" + (hi ? "h" : "l") + t[eB].Ticks + "_" + t[eA].Ticks;
                LnT(k, tA, a[eA], tB, a[eB], c, 1, LineStyle.Lines);
                TxtT(k + "t", s, tB, a[eB], c, 11, HorizontalAlignment.Center, hi ? VerticalAlignment.Top : VerticalAlignment.Bottom);
                found++;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Watermark
        // ════════════════════════════════════════════════════════════════════
        string TfText() => TfLabel(_chartSec > 0 ? _chartSec : 60);

        string WmText(bool sym, bool exch, bool tf, string extra)
        {
            string broker = "";
            if (exch) { try { broker = Account.BrokerName; } catch { } }
            string s = sym ? (exch && !string.IsNullOrEmpty(broker) ? broker + ":" + SymbolName : SymbolName) : "";
            if (tf) s = s == "" ? TfText() : s + " | " + TfText();
            return string.IsNullOrEmpty(extra) ? s : s == "" ? extra : s + " - " + extra;
        }

        // Son mumun NY tarihi: 05 Ocak 2026 · Pazartesi
        static string WmDate(DateTime t) => t.ToString("dd", INV) + " " + MONTH_NAMES[t.Month - 1] + " " + t.Year + " · " + DAY_NAMES[(int)t.DayOfWeek];

        static double WmFs(WmBoyu s) => s == WmBoyu.Çok_küçük ? 10 : s == WmBoyu.Küçük ? 12 : s == WmBoyu.Büyük ? 20 : s == WmBoyu.Çok_büyük ? 28 : 15;

        static HorizontalAlignment WmH(Konum k) => k == Konum.Sol ? HorizontalAlignment.Left : k == Konum.Sağ ? HorizontalAlignment.Right : HorizontalAlignment.Center;

        void Watermark()
        {
            if (WmtOn) Static("wmt", WmText(WmtSym, WmtExch, WmtTf, WmtTxt), VerticalAlignment.Top, WmH(WmtPos), WmtCol, WmFs(WmtSize), WmtFont);
            if (WmbOn)
            {
                string wb = WmText(WmbSym, WmbExch, WmbTf, WmbTxt);
                if (WmbDate && Bars.Count > 0)
                {
                    string d = WmDate(Bars.OpenTimes[Bars.Count - 1]);
                    wb = wb == "" ? d : wb + "\n" + d;
                }
                Static("wmb", wb, VerticalAlignment.Bottom, WmH(WmbPos), WmbCol, WmFs(WmbSize), WmbFont);
            }
        }

        void Static(string key, string text, VerticalAlignment v, HorizontalAlignment h, Color c, double fs, YaziTipi font)
        {
            string n = PFX + key;
            if (Need(n, $"{text}|{v}|{h}|{c}|{fs}|{font}"))
            {
                var t = Chart.DrawStaticText(n, text, v, h, c);
                t.FontSize = fs;
                if (font == YaziTipi.Monospace) t.FontFamily = "Consolas";
            }
        }
    }
}
