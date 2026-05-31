using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ZNext.Services;

internal sealed class AvatarPickerService
{
	private readonly AvatarService _avatarService;

	public AvatarPickerService(AvatarService avatarService)
	{
		_avatarService = avatarService;
	}

	public async Task<bool> PickAndSaveAvatarAsync(nint ownerHwnd)
	{
		FileOpenPicker picker = new FileOpenPicker
		{
			ViewMode = PickerViewMode.Thumbnail,
			SuggestedStartLocation = PickerLocationId.PicturesLibrary
		};
		picker.FileTypeFilter.Add(".png");
		picker.FileTypeFilter.Add(".jpg");
		picker.FileTypeFilter.Add(".jpeg");
		picker.FileTypeFilter.Add(".bmp");
		picker.FileTypeFilter.Add(".webp");

		InitializeWithWindow.Initialize(picker, ownerHwnd);
		StorageFile pickedFile = await picker.PickSingleFileAsync();
		if (pickedFile == null)
		{
			return false;
		}

		await _avatarService.SaveAvatarAsync(pickedFile);
		return true;
	}
}
