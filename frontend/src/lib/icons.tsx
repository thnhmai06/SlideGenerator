import {
  Add01Icon as _Add01,
  ArrowLeft01Icon as _ArrowLeft01,
  ArrowReloadHorizontalIcon as _ArrowReloadHorizontal,
  Cancel01Icon as _Cancel01,
  CheckmarkCircle01Icon as _CheckmarkCircle01,
  ChefHatIcon as _ChefHat,
  Comment01Icon as _Comment01,
  CpuSettingsIcon as _CpuSettings,
  Delete01Icon as _Delete01,
  ArrowUpRight01Icon as _ExternalLink,
  FileEditIcon as _FileEdit,
  FileExportIcon as _FileExport,
  FileSpreadsheetIcon as _FileSpreadsheet,
  File02Icon as _FileText,
  Flowchart01Icon as _Flowchart01,
  FolderOpenIcon as _FolderOpen,
  Github01Icon as _Github01,
  InformationCircleIcon as _InformationCircle,
  LaptopIcon as _Laptop,
  Layers01Icon as _Layers01,
  MapPinCheckIcon as _MapPinCheck,
  Moon01Icon as _Moon01,
  MoreVerticalIcon as _MoreVertical,
  PaintBrushIcon as _PaintBrush,
  PauseIcon as _Pause,
  PencilEdit01Icon as _PencilEdit01,
  PlayIcon as _Play,
  PresentationBarChart01Icon as _PresentationBarChart01,
  Redo02Icon as _Redo02,
  Refresh01Icon as _Refresh01,
  Search01Icon as _Search01,
  Settings02Icon as _Settings02,
  StopIcon as _Stop,
  Sun01Icon as _Sun01,
  TaskDone01Icon as _TaskDone01,
  Wifi01Icon as _Wifi01,
} from "@hugeicons/core-free-icons";
import { HugeiconsIcon, type HugeiconsIconProps } from "@hugeicons/react";
import { forwardRef } from "react";

type IconProps = Omit<HugeiconsIconProps, "icon">;

function makeIcon(iconData: HugeiconsIconProps["icon"]) {
  return forwardRef<SVGSVGElement, IconProps>((props, ref) => (
    <HugeiconsIcon ref={ref} icon={iconData} {...props} />
  ));
}

export const Add01Icon = makeIcon(_Add01);
export const ArrowLeft01Icon = makeIcon(_ArrowLeft01);
export const ArrowReloadHorizontalIcon = makeIcon(_ArrowReloadHorizontal);
export const Cancel01Icon = makeIcon(_Cancel01);
export const ChefHatIcon = makeIcon(_ChefHat);
export const CheckmarkCircle01Icon = makeIcon(_CheckmarkCircle01);
export const Comment01Icon = makeIcon(_Comment01);
export const CpuSettingsIcon = makeIcon(_CpuSettings);
export const Delete01Icon = makeIcon(_Delete01);
export const ExternalLinkIcon = makeIcon(_ExternalLink);
export const FileEditIcon = makeIcon(_FileEdit);
export const FileExportIcon = makeIcon(_FileExport);
export const FileSpreadsheetIcon = makeIcon(_FileSpreadsheet);
export const FileTextIcon = makeIcon(_FileText);
export const Flowchart01Icon = makeIcon(_Flowchart01);
export const FolderOpenIcon = makeIcon(_FolderOpen);
export const Github01Icon = makeIcon(_Github01);
export const InformationCircleIcon = makeIcon(_InformationCircle);
export const LaptopIcon = makeIcon(_Laptop);
export const Layers01Icon = makeIcon(_Layers01);
export const MapPinCheckIcon = makeIcon(_MapPinCheck);
export const Moon01Icon = makeIcon(_Moon01);
export const MoreVerticalIcon = makeIcon(_MoreVertical);
export const PaintBrushIcon = makeIcon(_PaintBrush);
export const PauseIcon = makeIcon(_Pause);
export const PencilEdit01Icon = makeIcon(_PencilEdit01);
export const PlayIcon = makeIcon(_Play);
export const PresentationBarChart01Icon = makeIcon(_PresentationBarChart01);
export const Redo02Icon = makeIcon(_Redo02);
export const Refresh01Icon = makeIcon(_Refresh01);
export const Search01Icon = makeIcon(_Search01);
export const Settings02Icon = makeIcon(_Settings02);
export const StopIcon = makeIcon(_Stop);
export const Sun01Icon = makeIcon(_Sun01);
export const TaskDone01Icon = makeIcon(_TaskDone01);
export const Wifi01Icon = makeIcon(_Wifi01);
