using System.Xml.Serialization;
namespace NcaaTranslator.Library
{
    [XmlRoot(ElementName = "Location")]
    public class Location
    {
        [XmlElement(ElementName = "X")]
        public string? X { get; set; }
        [XmlElement(ElementName = "Y")]
        public string? Y { get; set; }
    }

    [XmlRoot(ElementName = "Size")]
    public class Size
    {
        [XmlElement(ElementName = "Width")]
        public string? Width { get; set; }
        [XmlElement(ElementName = "Height")]
        public string? Height { get; set; }
    }

    [XmlRoot(ElementName = "ParentCanvas")]
    public class ParentCanvas
    {
        [XmlElement(ElementName = "Location")]
        public Location? Location { get; set; }
        [XmlElement(ElementName = "Size")]
        public Size? Size { get; set; }
        [XmlElement(ElementName = "X")]
        public string? X { get; set; }
        [XmlElement(ElementName = "Y")]
        public string? Y { get; set; }
        [XmlElement(ElementName = "Width")]
        public string? Width { get; set; }
        [XmlElement(ElementName = "Height")]
        public string? Height { get; set; }
    }

    [XmlRoot(ElementName = "BugIntroVideoRectangle")]
    public class BugIntroVideoRectangle
    {
        [XmlElement(ElementName = "Location")]
        public Location? Location { get; set; }
        [XmlElement(ElementName = "Size")]
        public Size? Size { get; set; }
        [XmlElement(ElementName = "X")]
        public string? X { get; set; }
        [XmlElement(ElementName = "Y")]
        public string? Y { get; set; }
        [XmlElement(ElementName = "Width")]
        public string? Width { get; set; }
        [XmlElement(ElementName = "Height")]
        public string? Height { get; set; }
    }

    [XmlRoot(ElementName = "Slice")]
    public class Slice
    {
        [XmlAttribute(AttributeName = "nil", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string? Nil { get; set; }
    }

    [XmlRoot(ElementName = "PieSlice")]
    public class PieSlice
    {
        [XmlElement(ElementName = "Slice")]
        public List<Slice>? Slice { get; set; }
    }

    [XmlRoot(ElementName = "Rule")]
    public class Rule
    {
        [XmlElement(ElementName = "ParamA_SRC")]
        public string? ParamA_SRC { get; set; }
        [XmlElement(ElementName = "ParamA_Index")]
        public string? ParamA_Index { get; set; }
        [XmlElement(ElementName = "ParamB_SRC")]
        public string? ParamB_SRC { get; set; }
        [XmlElement(ElementName = "ParamB_Index")]
        public string? ParamB_Index { get; set; }
        [XmlElement(ElementName = "ParamB_UserString")]
        public string? ParamB_UserString { get; set; }
        [XmlElement(ElementName = "Param_Operator")]
        public string? Param_Operator { get; set; }
        [XmlElement(ElementName = "ThenResult")]
        public string? ThenResult { get; set; }
        [XmlElement(ElementName = "ElseResult")]
        public string? ElseResult { get; set; }
        [XmlElement(ElementName = "ErrorResult")]
        public string? ErrorResult { get; set; }
        [XmlElement(ElementName = "ReturnTextValue")]
        public string? ReturnTextValue { get; set; }
    }

    [XmlRoot(ElementName = "ParentCanvasRectangle")]
    public class ParentCanvasRectangle
    {
        [XmlElement(ElementName = "Location")]
        public Location? Location { get; set; }
        [XmlElement(ElementName = "Size")]
        public Size? Size { get; set; }
        [XmlElement(ElementName = "X")]
        public string? X { get; set; }
        [XmlElement(ElementName = "Y")]
        public string? Y { get; set; }
        [XmlElement(ElementName = "Width")]
        public string? Width { get; set; }
        [XmlElement(ElementName = "Height")]
        public string? Height { get; set; }
    }

    [XmlRoot(ElementName = "GraphicObjParentSize")]
    public class GraphicObjParentSize
    {
        [XmlElement(ElementName = "Width")]
        public string? Width { get; set; }
        [XmlElement(ElementName = "Height")]
        public string? Height { get; set; }
    }

    [XmlRoot(ElementName = "GraphicObjFont")]
    public class GraphicObjFont
    {
        [XmlElement(ElementName = "FontFamilyName")]
        public string? FontFamilyName { get; set; }
        [XmlElement(ElementName = "FontSize")]
        public string? FontSize { get; set; }
        [XmlElement(ElementName = "FontStyle")]
        public string? FontStyle { get; set; }
    }

    [XmlRoot(ElementName = "GraphicObjLocation")]
    public class GraphicObjLocation
    {
        [XmlElement(ElementName = "X")]
        public string? X { get; set; }
        [XmlElement(ElementName = "Y")]
        public string? Y { get; set; }
    }

    [XmlRoot(ElementName = "GraphicObjRectangle")]
    public class GraphicObjRectangle
    {
        [XmlElement(ElementName = "Location")]
        public Location? Location { get; set; }
        [XmlElement(ElementName = "Size")]
        public Size? Size { get; set; }
        [XmlElement(ElementName = "X")]
        public string? X { get; set; }
        [XmlElement(ElementName = "Y")]
        public string? Y { get; set; }
        [XmlElement(ElementName = "Width")]
        public string? Width { get; set; }
        [XmlElement(ElementName = "Height")]
        public string? Height { get; set; }
    }

    [XmlRoot(ElementName = "ResizeGIFTo")]
    public class ResizeGIFTo
    {
        [XmlElement(ElementName = "Location")]
        public Location? Location { get; set; }
        [XmlElement(ElementName = "Size")]
        public Size? Size { get; set; }
        [XmlElement(ElementName = "X")]
        public string? X { get; set; }
        [XmlElement(ElementName = "Y")]
        public string? Y { get; set; }
        [XmlElement(ElementName = "Width")]
        public string? Width { get; set; }
        [XmlElement(ElementName = "Height")]
        public string? Height { get; set; }
    }

    [XmlRoot(ElementName = "Gif_Animatior")]
    public class Gif_Animatior
    {
        [XmlElement(ElementName = "BlockChildernUntilFinished")]
        public string? BlockChildernUntilFinished { get; set; }
        [XmlElement(ElementName = "AutoStart")]
        public string? AutoStart { get; set; }
        [XmlElement(ElementName = "KeepOpenAtEnd")]
        public string? KeepOpenAtEnd { get; set; }
        [XmlElement(ElementName = "DataEventSubscription")]
        public string? DataEventSubscription { get; set; }
        [XmlElement(ElementName = "LoopGif")]
        public string? LoopGif { get; set; }
        [XmlElement(ElementName = "GIFPath")]
        public string? GIFPath { get; set; }
        [XmlElement(ElementName = "ResizeGIFTo")]
        public ResizeGIFTo? ResizeGIFTo { get; set; }
    }

    [XmlRoot(ElementName = "clsGFXElement")]
    public class ClsGFXElement
    {
        [XmlElement(ElementName = "sStatCrewPlayerNumber")]
        public string? SStatCrewPlayerNumber { get; set; }
        [XmlElement(ElementName = "PieSlice")]
        public PieSlice? PieSlice { get; set; }
        [XmlElement(ElementName = "Rule")]
        public Rule? Rule { get; set; }
        [XmlElement(ElementName = "ParentCanvasRectangle")]
        public ParentCanvasRectangle? ParentCanvasRectangle { get; set; }
        [XmlElement(ElementName = "TextAlignment")]
        public string? TextAlignment { get; set; }
        [XmlElement(ElementName = "ShowDropShadow")]
        public string? ShowDropShadow { get; set; }
        [XmlElement(ElementName = "ShowAntiAlias")]
        public string? ShowAntiAlias { get; set; }
        [XmlElement(ElementName = "UseWordWrap")]
        public string? UseWordWrap { get; set; }
        [XmlElement(ElementName = "DrawBoundingRectangle")]
        public string? DrawBoundingRectangle { get; set; }
        [XmlElement(ElementName = "ForceUpperCase")]
        public string? ForceUpperCase { get; set; }
        [XmlElement(ElementName = "GraphicObjName")]
        public string? GraphicObjName { get; set; }
        [XmlElement(ElementName = "GraphicObjParentSize")]
        public GraphicObjParentSize? GraphicObjParentSize { get; set; }
        [XmlElement(ElementName = "GraphicObjType")]
        public string? GraphicObjType { get; set; }
        [XmlElement(ElementName = "SupportsLiveVideo")]
        public string? SupportsLiveVideo { get; set; }
        [XmlElement(ElementName = "SupportsBGVideoResize")]
        public string? SupportsBGVideoResize { get; set; }
        [XmlElement(ElementName = "GraphicObjFont")]
        public GraphicObjFont? GraphicObjFont { get; set; }
        [XmlElement(ElementName = "GraphicObjText")]
        public string? GraphicObjText { get; set; }
        [XmlElement(ElementName = "GraphicObjMediaPath")]
        public string? GraphicObjMediaPath { get; set; }
        [XmlElement(ElementName = "GraphicObjLocation")]
        public GraphicObjLocation? GraphicObjLocation { get; set; }
        [XmlElement(ElementName = "GraphicObjRectangle")]
        public GraphicObjRectangle? GraphicObjRectangle { get; set; }
        [XmlElement(ElementName = "GraphicObjTextColor")]
        public string? GraphicObjTextColor { get; set; }
        [XmlElement(ElementName = "GraphicObjTextColor2")]
        public string? GraphicObjTextColor2 { get; set; }
        [XmlElement(ElementName = "GraphicObjTextStrokeColor")]
        public string? GraphicObjTextStrokeColor { get; set; }
        [XmlElement(ElementName = "DynamicSRCDataField")]
        public string? DynamicSRCDataField { get; set; }
        [XmlElement(ElementName = "DynamicSRCAttrib")]
        public string? DynamicSRCAttrib { get; set; }
        [XmlElement(ElementName = "PNG_Loop")]
        public string? PNG_Loop { get; set; }
        [XmlElement(ElementName = "WebVariableNumber")]
        public string? WebVariableNumber { get; set; }
        [XmlElement(ElementName = "SystemDateTimeSelection")]
        public string? SystemDateTimeSelection { get; set; }
        [XmlElement(ElementName = "BaseTimeTicks")]
        public string? BaseTimeTicks { get; set; }
        [XmlElement(ElementName = "TimeLine_AppearDelay_ms")]
        public string? TimeLine_AppearDelay_ms { get; set; }
        [XmlElement(ElementName = "Gif_Animatior")]
        public Gif_Animatior? Gif_Animatior { get; set; }
    }

    [XmlRoot(ElementName = "GfxElements")]
    public class GfxElements
    {
        [XmlElement(ElementName = "clsGFXElement")]
        public List<ClsGFXElement>? ClsGFXElement { get; set; }
    }

    [XmlRoot(ElementName = "clsGFXTemplate")]
    public class ClsGFXTemplate
    {
        [XmlElement(ElementName = "ParentCanvas")]
        public ParentCanvas? ParentCanvas { get; set; }
        [XmlElement(ElementName = "ParentCanvasWallPaper")]
        public string? ParentCanvasWallPaper { get; set; }
        [XmlElement(ElementName = "BugIntroVideo")]
        public string? BugIntroVideo { get; set; }
        [XmlElement(ElementName = "BugIntroVideoSampleFrame")]
        public string? BugIntroVideoSampleFrame { get; set; }
        [XmlElement(ElementName = "BugIntroVideoLoop")]
        public string? BugIntroVideoLoop { get; set; }
        [XmlElement(ElementName = "BugIntroVideoRectangle")]
        public BugIntroVideoRectangle? BugIntroVideoRectangle { get; set; }
        [XmlElement(ElementName = "GfxElements")]
        public GfxElements? GfxElements { get; set; }
    }

}