using Npgsql;
using NAudio.Wave;
using AudioStreamingApi.Models.DbModels;
using AudioStreamingApi.Models;
using System.Security.Claims;
using NLayer.NAudioSupport;
using AudioStreamingApi.Models.PseudoDbModels;

namespace AudioStreamingApi.Components
{
	public class HttpControllersMethods
	{
		public HttpControllersMethods()
		{
		}

        public static string[] SplitFileNameWithContentTypeIntoTwoParts(string fileName)
        {
            string[] fileNameParts = fileName.Split(".");
            string[] twoFileNameParts = new string[2];

            twoFileNameParts[0] = fileNameParts[0];

            for (int i = 0; i < fileNameParts.Length - 1; i++)
            {
                string item = fileNameParts[i];

                twoFileNameParts[0] += $".{item}";
            }

            twoFileNameParts[1] = fileNameParts[fileNameParts.Length - 1];

            return twoFileNameParts;
        }

        public static bool CheckIfImageDataIsNotNullAndSaveToDb(IFormFile? image, NpgsqlConnection npgsqlDbConnection, out int? imageIdInDb)
		{
            if (!CheckIfImageDataIsNotNull(image))
            {
                imageIdInDb = null;

                return false;
            }
            else
            {
                imageIdInDb = DbMethods.CreateAFile(image, npgsqlDbConnection);

                return true;
            }
        }

        public static bool CheckIfImageDataIsNotNull(IFormFile? image)
        {
            string[] allowedImageContentTypes = new string[] { "image/jpeg", "image/png", "image/heif", "image/tiff" };
            string[] allowedImageTypes = new string[] { "jpg", "jpeg", "png", "heif", "tiff" };

            if (image == null)
            {
                return false;
            }
            else if (!allowedImageContentTypes.Contains(image.ContentType))
            {
                throw new Exception($"Invalid image format, please use {String.Join(", ", allowedImageContentTypes)}");
            }
            else if (!allowedImageTypes.Contains(SplitFileNameWithContentTypeIntoTwoParts(image.FileName)[1]))
            {
                throw new Exception($"Invalid image type, please use {String.Join(", ", allowedImageTypes)}");
            }
            else
            {
                return true;
            }
        }

        public static void CheckIfSongContentIsinRightFormatOrThrowError(IFormFile file, out TimeSpan totalTime)
        {
            string tempFilePath = Path.GetTempFileName();

            using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            var invalidFileError = new Exception("The file is incorrect");

            var mp3ReaderBuilder = new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));

            switch (file.ContentType)
            {
                case "audio/mpeg":
                    using (var mp3Reader = new Mp3FileReaderBase(tempFilePath, mp3ReaderBuilder))
                    {
                        if (mp3Reader.CanRead == false)
                        {
                            throw invalidFileError;
                        }

                        totalTime = mp3Reader.TotalTime;
                    }
                    break;
                default:
                    throw new Exception("Invalid song format, please use mp3");
            }

            File.Delete(tempFilePath);
        }

        public static bool CheckIfVariablesAreNotNull(params dynamic?[] vars)
        {
            foreach (var var in vars)
            {
                if (var == null)
                {
                    return false;
                }
            }
            return true;
        }

        public static AudioStringContentAndIsLastSongPart TrimAudioFileForStreaming(DbFile audioFile, TimeSpan cutFromStart, TimeSpan maxCutFromEnd)
        {
            AudioStringContentAndIsLastSongPart returnObj;

            var mp3ReaderBuilder = new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));

            switch (audioFile.Type)
            {
                case "audio/mpeg":
                    using (Mp3FileReaderBase reader = new Mp3FileReaderBase(GetFilePathForUserFiles(audioFile.Path), mp3ReaderBuilder))
                    {
                        if (cutFromStart <= reader.TotalTime)
                        {
                            bool isLastSongPart = false;
                            TimeSpan cutFromEnd = maxCutFromEnd;

                            if (maxCutFromEnd >= reader.TotalTime)
                            {
                                isLastSongPart = true;
                                cutFromEnd = reader.TotalTime;
                            }

                            string contentRaw = TrimMp3(reader, cutFromStart, cutFromEnd, out long totalSongBytes);

                            returnObj = new AudioStringContentAndIsLastSongPart(contentRaw , isLastSongPart);
                        }
                        else
                        {
                            throw new Exception("The time is out of range");
                        }
                    }
                    break;
                default:
                    throw new Exception("Invalid song format, use audio/mpeg");
            }

            return returnObj;
        }

        public static string TrimFile(byte[] audioContent, int startPos, int endPos)
        {
            try
            {
                int bytesToCopy = endPos - startPos;

                byte[] returnArray = new byte[bytesToCopy];

                Buffer.BlockCopy(audioContent, startPos, returnArray, 0, bytesToCopy);

                return Convert.ToBase64String(returnArray);
            } catch (Exception ex)
            {
                return "Error trimming the file";
            }
        }

        public static string TrimMp3(Mp3FileReaderBase reader, TimeSpan? begin, TimeSpan? end, out long totalSongBytes)
        {
            if (begin.HasValue && end.HasValue && begin > end)
                throw new ArgumentOutOfRangeException("end", "end should be greater than begin");            

            List<byte> bytesToReturn = new List<byte>();

            long totalSBytes = 0;

            Mp3Frame frame;

            while ((frame = reader.ReadNextFrame()) != null)
                if (reader.CurrentTime >= begin || !begin.HasValue)
                {
                    var frameBytes = frame.RawData;
                    if (reader.CurrentTime <= end || !end.HasValue)
                    {
                        bytesToReturn.AddRange(frameBytes);

                        totalSBytes += frameBytes.Length;
                    }
                    else totalSBytes += frameBytes.Length;
                }
            

            totalSongBytes = totalSBytes;

            return Convert.ToBase64String(bytesToReturn.ToArray());
        }

        public static int GetUserIdFromHttpContextIfAuthorized(HttpContext httpContext)
        {
            string token = JwtIssuer.GetTokenFromHeaders(httpContext.Request.Headers);

            JwtIssuer.ValidateToken(token, out List<Claim> claims);

            int userId = Convert.ToInt32(claims[0].Value);

            return userId;
        }

        public static TimeSpan GetSongTotalTime(Song song)
        {
           TimeSpan timeSpan;

            var mp3ReaderBuilder = new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));

            switch (song.Content.Type)
            {
                case "audio/mpeg":
                    using (Mp3FileReaderBase reader = new Mp3FileReaderBase(GetFilePathForUserFiles(song.Content.Path), mp3ReaderBuilder))
                    {
                        timeSpan = reader.TotalTime;
                    }
                    break;
                default:
                    throw new Exception("Invalid song format");
                    break;
            }

            return timeSpan;
        }

        public static string GetContentInStrFromFormFile(IFormFile file)
        {
            return Convert.ToBase64String(GetContentFromFormFile(file));
        }

        public static byte[] GetContentFromFormFile(IFormFile file)
        {
            using (var memoryStream = new MemoryStream()) 
            {
                file.CopyTo(memoryStream);

                return memoryStream.ToArray();
            }
        }

        public static string GetFilePathForUserFiles(string fileName)
        {
            return $"{Constants.UserFilesFolder}/{fileName}";
        }

        public static byte[] GetBytesFromDbFile(DbFile dbFile)
        {
            return File.ReadAllBytes(GetFilePathForUserFiles(dbFile.Path));
        }

        public static string GetBase64StringFromDbFile(DbFile dbFile)
        {
            return Convert.ToBase64String(GetBytesFromDbFile(dbFile));
        }

        public static DbFileWithContent ConvertDbFileToFormatForUsers(DbFile dbFile)
        {
            return new DbFileWithContent(GetBase64StringFromDbFile(dbFile), dbFile.Type);
        }

        public static UserForHimself ConvertUserToFormatForHimself(User user)
        {
            UserForUsers userForUsers = ConvertUserToFormatForUsers(user);

            return new UserForHimself(userForUsers.Id, userForUsers.Name, user.Email, user.IsTwoFactorAuthActive, userForUsers.Avatar);
        }

        public static UserForUsers ConvertUserToFormatForUsers(User user)
        {
            DbFileWithContent? avatar = null;

            if (user.Avatar != null)
            {
                avatar = ConvertDbFileToFormatForUsers(user.Avatar);
            }

            return new UserForUsers(user.Id, user.Name, avatar);
        }

        public static DiscForUsers ConvertDiscToFormatForUsers(Disc disc)
        {
            UserForUsers? artist = null;
            if(disc.Artist != null)
            {
                artist = ConvertUserToFormatForUsers(disc.Artist);
            }

            DbFileWithContent? cover = null;
            if (disc.Cover != null)
            {
                cover = ConvertDbFileToFormatForUsers(disc.Cover);
            }

            List<SongForUsers> songs = new List<SongForUsers>();
            if (disc.Songs != null) {
                foreach (var song in disc.Songs)
                {
                    songs.Add(ConvertSongToFormatForUsers(song));
                }
            }

            return new DiscForUsers(disc.Id, disc.Name, disc.CreatedAt, disc.ArtistId, artist, cover, songs);
        }

        public static SongForUsers ConvertSongToFormatForUsers(Song song)
        {
            DiscForUsers? disc = null;
            if (song.Disc != null)
            {
                disc = ConvertDiscToFormatForUsers(song.Disc);
            }

            return new SongForUsers(song.Id, song.Name, song.TotalTime, song.DiscId, disc);
        }

        public static List<SongForUsers> GetSongsForUserFromItemsFromEnum(List<ItemFromEnum> itemsList, NpgsqlConnection npgsqlDbConnection)
        {
            List<SongForUsers> songs = new List<SongForUsers>();

            foreach (var item in itemsList)
            {
                try
                {
                    songs.Add(ConvertSongToFormatForUsers(DbMethods.GetSongById(item.ItemId, npgsqlDbConnection)));
                } catch (Exception ex)
                {
                    throw new Exception($"Song with id {item.ItemId} wasn't found");
                }
            }

            return songs;
        }

        public static List<SongForUsers> GetSongsForUserFromItemsFromEnum(List<ItemFromEnumWithViews> itemsList, NpgsqlConnection npgsqlDbConnection)
        {
            List<SongForUsers> songs = new List<SongForUsers>();

            foreach (var item in itemsList)
            {
                try
                {
                    songs.Add(ConvertSongToFormatForUsers(DbMethods.GetSongById(item.ItemId, npgsqlDbConnection)));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Song with id {item.ItemId} wasn't found");
                }
            }

            return songs;
        }

        public static List<SongForUsers> GetSongsForUserFromItemsFromEnum(List<ItemFromEnumWithViewsAndIndexRating> itemsList, NpgsqlConnection npgsqlDbConnection)
        {
            List<SongForUsers> songs = new List<SongForUsers>();

            foreach (var item in itemsList)
            {
                try
                {
                    songs.Add(ConvertSongToFormatForUsers(DbMethods.GetSongById(item.ItemId, npgsqlDbConnection)));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Song with id {item.ItemId} wasn't found");
                }
            }

            return songs;
        }

        public static List<DiscForUsers> GetDiscographyForUserFromItemsFromEnum(List<ItemFromEnumWithViews> itemsList, NpgsqlConnection npgsqlDbConnection)
        {
            List<DiscForUsers> discs = new List<DiscForUsers>();

            foreach (var item in itemsList)
            {
                try
                {
                    discs.Add(ConvertDiscToFormatForUsers(DbMethods.GetDiscById(item.ItemId, npgsqlDbConnection)));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Song with id {item.ItemId} wasn't found");
                }
            }

            return discs;
        }

        public static List<DiscForUsers> GetDiscographyForUserFromItemsFromEnum(List<ItemFromEnum> itemsList, NpgsqlConnection npgsqlDbConnection)
        {
            List<DiscForUsers> discs = new List<DiscForUsers>();

            foreach (var item in itemsList)
            {
                try
                {
                    discs.Add(ConvertDiscToFormatForUsers(DbMethods.GetDiscById(item.ItemId, npgsqlDbConnection)));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Song with id {item.ItemId} wasn't found");
                }
            }

            return discs;
        }

        public static List<DiscForUsers> GetDiscographyForUserFromDiscs(List<Disc> itemsList, NpgsqlConnection npgsqlDbConnection)
        {
            List<DiscForUsers> discs = new List<DiscForUsers>();

            foreach (var item in itemsList)
            {
                try
                {
                    discs.Add(ConvertDiscToFormatForUsers(DbMethods.GetDiscById(item.Id, npgsqlDbConnection)));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Disc with id {item.Id} wasn't found");
                }
            }

            return discs;
        }

        public static List<DiscForUsers> GetDiscographyForUserFromDiscs(List<ItemFromEnum> itemsList, NpgsqlConnection npgsqlDbConnection)
        {
            List<DiscForUsers> discs = new List<DiscForUsers>();

            foreach (var item in itemsList)
            {
                try
                {
                    discs.Add(ConvertDiscToFormatForUsers(DbMethods.GetDiscById(item.ItemId, npgsqlDbConnection)));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Disc with id {item.ItemId} wasn't found");
                }
            }

            return discs;
        }

        public static List<UserForUsers> GetUsersForUserFromItemsFromEnum(List<ItemFromEnumWithViews> itemsList, NpgsqlConnection npgsqlDbConnection)
        {
            List<UserForUsers> users = new List<UserForUsers>();

            foreach (var item in itemsList)
            {
                try
                {
                    users.Add(ConvertUserToFormatForUsers(DbMethods.GetUserById(item.ItemId, npgsqlDbConnection)));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Song with id {item.ItemId} wasn't found");
                }
            }

            return users;
        }

        public static List<UserForUsers> GetUsersForUserFromItemsFromEnum(List<ItemFromEnum> itemsList, NpgsqlConnection npgsqlDbConnection)
        {
            List<UserForUsers> users = new List<UserForUsers>();

            foreach (var item in itemsList)
            {
                try
                {
                    users.Add(ConvertUserToFormatForUsers(DbMethods.GetUserById(item.ItemId, npgsqlDbConnection)));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Song with id {item.ItemId} wasn't found");
                }
            }

            return users;
        }

        public static void DeleteFileFromStorage(string fileName)
        {
            File.Delete(GetFilePathForUserFiles(fileName));
        }
    }
}

