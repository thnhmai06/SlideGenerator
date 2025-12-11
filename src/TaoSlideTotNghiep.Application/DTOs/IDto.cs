namespace TaoSlideTotNghiep.Application.DTOs;

internal interface IDto;

internal interface IImageDto : IDto,
    IFilePathBased;

internal interface IPresentationDto : IDto;

internal interface IDownloadDto : IDto,
    IFilePathBased;

internal interface ISheetDto : IDto,
    IFilePathBased;