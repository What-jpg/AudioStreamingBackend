using Dapper;
using Npgsql;
using AudioStreamingApi.Models;
using AudioStreamingApi.Models.DbModels;
using AudioStreamingApi.Models.PseudoDbModels;
using System.Text.RegularExpressions;

namespace AudioStreamingApi.Components
{
	public class DbMethods
	{
		public DbMethods()
		{
		}

		public static string CreateAcceptableNulluableInt(int? num)
		{
			string acceptable = num.ToString();

			if(num == null)
			{
				acceptable = "null";
			}

			return acceptable;
		}

        public static string CreateAcceptableString(string str)
        {
            return str.Replace("'", "''").Replace("\"", "\"\"");
        }

        public static string CreateAcceptableNulluableStr(string? str)
        {
            string acceptable = $"\"{str}\"";

            if (str == null)
            {
                acceptable = "null";
            }

            return acceptable;
        }

        public static int CreateAFile(IFormFile file, NpgsqlConnection npgsqlDbConnection)
        {
            string[] inputFileNameParts = HttpControllersMethods.SplitFileNameWithContentTypeIntoTwoParts(file.FileName);
            string filePath = $"{inputFileNameParts[0]}-{DateTime.UtcNow.ToFileTimeUtc()}.{inputFileNameParts[1]}";
            string fullFilePath = HttpControllersMethods.GetFilePathForUserFiles(filePath);

            using (FileStream fileStream = new FileStream(fullFilePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            var setAvatarSql = $"INSERT INTO \"DbFiles\" (path, type) VALUES ('{CreateAcceptableString(filePath)}', '{file.ContentType}') RETURNING id";

            int id = npgsqlDbConnection.QuerySingle<int>(setAvatarSql);

            return id;
        }

        public static int CreateAFile(FileForRedisAuth file, NpgsqlConnection npgsqlDbConnection)
        {
            string[] inputFileNameParts = HttpControllersMethods.SplitFileNameWithContentTypeIntoTwoParts(file.FileName);
            string filePath = $"{inputFileNameParts[0]}-{DateTime.UtcNow.ToFileTimeUtc()}.{inputFileNameParts[1]}";
            string fullFilePath = HttpControllersMethods.GetFilePathForUserFiles(filePath);

            using (MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(file.Content)))
            using (FileStream fileStream = new FileStream(fullFilePath, FileMode.Create))
            {
                memoryStream.CopyTo(fileStream);
            }

            var setAvatarSql = $"INSERT INTO \"DbFiles\" (path, type) VALUES ('{CreateAcceptableString(filePath)}', '{file.ContentType}') RETURNING id";

            int id = npgsqlDbConnection.QuerySingle<int>(setAvatarSql);

            return id;
        }

        public static int CreateAnUser(string name, string email, string hashedPassword, int? avatarId, NpgsqlConnection npgsqlDbConnection)
        {
            var setUserSql = $"INSERT INTO \"Users\" (name, email, \"hashedPassword\", \"avatarId\") VALUES ('{name}', '{email}', '{hashedPassword}', {DbMethods.CreateAcceptableNulluableInt(avatarId)}) RETURNING \"id\"";

            int id = npgsqlDbConnection.QuerySingle<int>(setUserSql);

            return id;
        }

        public static int CreateAnUser(string name, string email, string hashedPassword, int? avatarId, bool isTwoFactorAuthenticationActive, NpgsqlConnection npgsqlDbConnection)
        {
            var setUserSql = $"INSERT INTO \"Users\" (name, email, \"hashedPassword\", \"avatarId\", \"isTwoFactorAuthActive\") VALUES ('{name}', '{email}', '{hashedPassword}', {DbMethods.CreateAcceptableNulluableInt(avatarId)}, '{isTwoFactorAuthenticationActive}') RETURNING \"id\"";

            int id = npgsqlDbConnection.QuerySingle<int>(setUserSql);

            return id;
        }


        public static int CreateAnAlbum(int artistId, string name, int? coverId, NpgsqlConnection npgsqlDbConnection)
        {
            var setAvatarSql = $"INSERT INTO \"Discography\" (\"artistId\", name, \"coverId\") VALUES ('{artistId}', '{CreateAcceptableString(name)}', {DbMethods.CreateAcceptableNulluableInt(coverId)}) RETURNING id";

            int id = npgsqlDbConnection.QuerySingle<int>(setAvatarSql);

            return id;
        }

        public static int CreateASong(string name, int discId, TimeSpan totalTime, int contentId, NpgsqlConnection npgsqlDbConnection)
        {
            var setAvatarSql = $"INSERT INTO \"Songs\" (name, \"totalTime\", \"discId\", \"contentId\") VALUES ('{CreateAcceptableString(name)}', '{totalTime}', '{discId}', '{contentId}') RETURNING id";

            int id = npgsqlDbConnection.QuerySingle<int>(setAvatarSql);

            return id;
        }

        public static int CreateASong(string name, int discId, TimeSpan totalTime, IFormFile contentFile, NpgsqlConnection npgsqlDbConnection)
        {
            int contentId = CreateAFile(contentFile, npgsqlDbConnection);

            var setAvatarSql = $"INSERT INTO \"Songs\" (name, \"totalTime\", \"discId\", \"contentId\") VALUES ('{CreateAcceptableString(name)}', '{totalTime}', '{discId}', '{contentId}') RETURNING id";

            int id = npgsqlDbConnection.QuerySingle<int>(setAvatarSql);

            return id;
        }

        public static void UpdateUser(User newUser, NpgsqlConnection npgsqlDbConnection) {
            var updateUserSql = $"UPDATE \"Users\" SET name = '{CreateAcceptableString(newUser.Name)}', email = '{newUser.Email}', \"hashedPassword\" = '{CreateAcceptableString(newUser.HashedPassword)}', \"isTwoFactorAuthActive\" = '{newUser.IsTwoFactorAuthActive}', \"avatarId\" = {CreateAcceptableNulluableInt(newUser.AvatarId)}  WHERE id = '{newUser.Id}' ";

            npgsqlDbConnection.Query(updateUserSql);
        }

        public static void UpdateSong(Song newSong, NpgsqlConnection npgsqlDbConnection)
        {
            var updateUserSql = $"UPDATE \"Songs\" SET name = '{CreateAcceptableString(newSong.Name)}', \"contentId\" = '{newSong.ContentId}'  WHERE id = '{newSong.Id}' ";

            npgsqlDbConnection.Query(updateUserSql);
        }

        public static void UpdateDisc(Disc newDisc, NpgsqlConnection npgsqlDbConnection)
        {
            var updateUserSql = $"UPDATE \"Discography\" SET name = '{CreateAcceptableString(newDisc.Name)}', \"coverId\" = {CreateAcceptableNulluableInt(newDisc.CoverId)}  WHERE id = '{newDisc.Id}' ";

            npgsqlDbConnection.Query(updateUserSql);
        }

        public static int CreateSongListened(int songId, int listenerId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"INSERT INTO \"SongsListened\" (\"listenerId\", \"songId\") VALUES ('{listenerId}', '{songId}') RETURNING id";

            var songListenedId = npgsqlDbConnection.QuerySingleOrDefault<int>(sql);

            return songListenedId;
        }

        public static List<ItemFromEnumWithViews> GetTheMostPopularSongs(NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT s2.id AS \"itemId\", SUM(sl2.count) AS views FROM  \"Songs\" s2 INNER JOIN \"Discography\" d ON d.id = s2.\"discId\", LATERAL (SELECT COUNT(*) FROM \"SongsListened\" s WHERE s.\"songId\" = s2.id) sl2 GROUP BY s2.id ORDER BY views DESC LIMIT 30";
            List<ItemFromEnumWithViews> theMostPopularItems = npgsqlDbConnection.Query<ItemFromEnumWithViews>(sql).ToList();

            return theMostPopularItems;
        }

        public static List<ItemFromEnumWithViews> GetTheMostPopularSongsFor7Days(NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT s2.id AS \"itemId\", SUM(sl2.count) AS views FROM  \"Songs\" s2 INNER JOIN \"Discography\" d ON d.id = s2.\"discId\", LATERAL (SELECT COUNT(*) FROM \"SongsListened\" s WHERE s.\"songId\" = s2.id AND s.\"listenedAt\" >= (NOW()::TIMESTAMP - interval '1' day * '7')) sl2 GROUP BY s2.id ORDER BY views DESC LIMIT 30";

            List<ItemFromEnumWithViews> theMostPopularItems = npgsqlDbConnection.Query<ItemFromEnumWithViews>(sql).ToList();

            return theMostPopularItems;
        }

        public static List<ItemFromEnumWithViews> GetTheMostPopularDiscography(NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT d.id AS \"itemId\", SUM(sl2.count) AS views FROM  \"Songs\" s2 INNER JOIN \"Discography\" d ON d.id = s2.\"discId\", LATERAL (SELECT COUNT(*) FROM \"SongsListened\" s WHERE s.\"songId\" = s2.id) sl2 GROUP BY d.id ORDER BY views DESC LIMIT 30";

            List<ItemFromEnumWithViews> theMostPopularItems = npgsqlDbConnection.Query<ItemFromEnumWithViews>(sql).ToList();

            return theMostPopularItems;
        }

        public static List<ItemFromEnumWithViews> GetTheMostPopularDiscographyFor7Days(NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT d.id AS \"itemId\", SUM(sl2.count) AS views FROM  \"Songs\" s2 INNER JOIN \"Discography\" d ON d.id = s2.\"discId\", LATERAL (SELECT COUNT(*) FROM \"SongsListened\" s WHERE s.\"songId\" = s2.id AND s.\"listenedAt\" >= (NOW()::TIMESTAMP - interval '1' day * '7')) sl2 GROUP BY d.id ORDER BY views DESC LIMIT 30";

            List<ItemFromEnumWithViews> theMostPopularItems = npgsqlDbConnection.Query<ItemFromEnumWithViews>(sql).ToList();

            return theMostPopularItems;
        }

        public static List<ItemFromEnumWithViews> GetTheMostPopularArtists(NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT d.\"artistId\" AS \"itemId\", SUM(sl2.count) AS views FROM  \"Songs\" s2 INNER JOIN \"Discography\" d ON d.id = s2.\"discId\", LATERAL (SELECT COUNT(*) FROM \"SongsListened\" s WHERE s.\"songId\" = s2.id AND s.\"listenedAt\" >= (NOW()::TIMESTAMP - interval '1' day * '7')) sl2 GROUP BY d.\"artistId\" ORDER BY views DESC LIMIT 30";

            List<ItemFromEnumWithViews> theMostPopularItems = npgsqlDbConnection.Query<ItemFromEnumWithViews>(sql).ToList();

            return theMostPopularItems;
        }

        public static List<ItemFromEnumWithViews> GetTheMostPopularArtistsFor7Days(NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT d.\"artistId\" AS \"itemId\", SUM(sl2.count) AS views FROM  \"Songs\" s2 INNER JOIN \"Discography\" d ON d.id = s2.\"discId\", LATERAL (SELECT COUNT(*) FROM \"SongsListened\" s WHERE s.\"songId\" = s2.id AND s.\"listenedAt\" >= (NOW()::TIMESTAMP - interval '1' day * '7')) sl2 GROUP BY d.\"artistId\" ORDER BY views DESC LIMIT 30";

            List<ItemFromEnumWithViews> theMostPopularItems = npgsqlDbConnection.Query<ItemFromEnumWithViews>(sql).ToList();

            return theMostPopularItems;
        }

        public static List<ItemFromEnumWithViewsAndIndexRating> GetRecommendedSongs(NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT sl1.\"songId\" AS \"itemId\", sl2.count AS views, ((1 / (CAST(EXTRACT(EPOCH FROM (INTERVAL '1' DAY * '7')) AS FLOAT) / CAST(EXTRACT(EPOCH FROM (INTERVAL '1' DAY * '7' - (NOW()::TIMESTAMP - s.\"createdAt\"))) AS FLOAT))) + (sl2.count / (SELECT sl4.count AS views FROM \"SongsListened\" sl3, LATERAL (SELECT COUNT(*) FROM \"SongsListened\" s WHERE sl3.\"songId\" = s.\"songId\") sl4 GROUP BY views, \"songId\" ORDER BY views DESC LIMIT 1))) AS \"indexRating\" FROM \"SongsListened\" sl1 INNER JOIN \"Songs\" s ON sl1.\"songId\" = s.id, LATERAL (SELECT COUNT(*) FROM \"SongsListened\" sl2 WHERE sl1.\"songId\" = sl2.\"songId\") sl2 GROUP BY views, s.id, sl1.\"songId\" ORDER BY \"indexRating\" DESC LIMIT 30";

            List<ItemFromEnumWithViewsAndIndexRating> recomendedSongs = npgsqlDbConnection.Query<ItemFromEnumWithViewsAndIndexRating>(sql).ToList();

            return recomendedSongs;
        }

        public static Song? GetSongByIdWithoutDisc(int id, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT * FROM \"Songs\" s WHERE s.id = '{id}' LIMIT '1'";

            Song? song = npgsqlDbConnection.Query<Song>(sql).FirstOrDefault();

            if (song != null)
            {
                song.Content = GetDbFileById(song.ContentId, npgsqlDbConnection);
            }

            return song;
        }

        public static Song? GetSongById(int id, NpgsqlConnection npgsqlDbConnection)
        {
            Song? song = GetSongByIdWithoutDisc(id, npgsqlDbConnection);

            if (song != null)
            {
                song.Disc = GetDiscByIdWithoutSongs(song.DiscId, npgsqlDbConnection);
            }

            return song;
        }

        public static Disc? GetDiscByIdWithoutSongs(int id, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT * FROM \"Discography\" d WHERE d.id = '{id}' LIMIT '1'";

            Disc? disc = npgsqlDbConnection.Query<Disc>(sql).FirstOrDefault();

            if (disc != null)
            {
                if (disc.CoverId != null)
                {
                    disc.Cover = GetDbFileById(disc.CoverId!.Value, npgsqlDbConnection);
                }
                disc.Artist = GetUserById(disc.ArtistId, npgsqlDbConnection);
            }

            return disc;
        }

        public static Disc? GetDiscById(int id, NpgsqlConnection npgsqlDbConnection)
        {
            Disc? disc = GetDiscByIdWithoutSongs(id, npgsqlDbConnection);

            if (disc != null) {
                disc.Songs = GetDiscSongs(id, npgsqlDbConnection);
            }

            return disc;
        }

        public static User? GetUserById(int id, NpgsqlConnection npgsqlDbConnection)
        {

            var sql = $"SELECT a.id, a.name, a.email, a.\"hashedPassword\", a.\"isTwoFactorAuthActive\", a.\"avatarId\" FROM \"Users\" a WHERE a.id = '{id}' LIMIT '1'";

            User? user = npgsqlDbConnection.Query<User>(sql).SingleOrDefault();

            if (user.AvatarId != null)
            {
                user.Avatar = GetDbFileById(user.AvatarId!.Value, npgsqlDbConnection);
            }

            return user;
        }

        public static Song? GetSongByIdWithoutJoins(int id, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT * FROM \"Songs\" s WHERE s.id = '{id}' LIMIT '1'";

            Song? song = npgsqlDbConnection.QuerySingleOrDefault<Song>(sql);

            return song;
        }

        public static Disc? GetDiscByIdWithoutJoins(int id, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT * FROM \"Discography\" d WHERE d.id = '{id}' LIMIT '1'";

            Disc? disc = npgsqlDbConnection.QuerySingleOrDefault<Disc>(sql);

            return disc;
        }

        public static User? GetUserByIdWithoutJoins(int id, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT * From \"Users\" a WHERE a.id = '{id}' LIMIT '1'";

            User? user = npgsqlDbConnection.QuerySingleOrDefault<User>(sql);

            return user;
        }

        public static DbFile? GetDbFileById(int id, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT * FROM \"DbFiles\" f WHERE f.id = '{id}' LIMIT '1'";

            DbFile? file = npgsqlDbConnection.QuerySingleOrDefault<DbFile>(sql);

            return file;
        }

        public static ArtistFollower? GetArtistFollowerByIdsWithoutJoins(int followerId, int artistId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT * FROM \"ArtistsFollowers\" af WHERE af.\"followerId\" = '{followerId}' AND af.\"elementId\" = '{artistId}' LIMIT '1'";

            ArtistFollower? artistFollower = npgsqlDbConnection.QuerySingleOrDefault<ArtistFollower>(sql);

            return artistFollower;
        }

        public static SongFollower? GetSongFollowerByIdsWithoutJoins(int followerId, int songId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT * FROM \"SongsFollowers\" sf WHERE sf.\"followerId\" = '{followerId}' AND sf.\"elementId\" = '{songId}' LIMIT '1'";

            SongFollower? songFollower = npgsqlDbConnection.QuerySingleOrDefault<SongFollower>(sql);

            return songFollower;
        }

        public static DiscFollower? GetDiscFollowerByIdsWithoutJoins(int followerId, int discId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT * FROM \"DiscographyFollowers\" df WHERE df.\"followerId\" = '{followerId}' AND df.\"elementId\" = '{discId}' LIMIT '1'";

            DiscFollower? discFollower = npgsqlDbConnection.QuerySingleOrDefault<DiscFollower>(sql);

            return discFollower;
        }

        public static List<Disc> GetUserDiscography(int artistId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT * FROM \"Discography\" d WHERE d.\"artistId\" = '{artistId}'";

            List<Disc> discography = npgsqlDbConnection.Query<Disc>(sql).ToList();

            return discography;
        }

        public static List<Song> GetDiscSongs(int discId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT * FROM \"Songs\" s WHERE s.\"discId\" = '{discId}' ORDER BY s.\"createdAt\" ";

            List<Song> songs = npgsqlDbConnection.Query<Song>(sql).ToList();

            return songs;
        }

        public static List<Song> GetUserSongs(int artistId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT s.id, s.name, s.\"createdAt\", s.\"discId\", s.\"contentId\" FROM \"Songs\" s INNER JOIN \"Discography\" d ON d.id = s.\"discId\" INNER JOIN \"Users\" ar ON ar.id = d.\"artistId\" WHERE ar.id = '{artistId}'";

            List<Song> songs = npgsqlDbConnection.Query<Song>(sql).ToList();

            return songs;
        }

        public static List<ItemFromEnumWithViews> GetListenerPopularSongs(int listenerId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT \"songId\" as \"itemId\", sl2.count AS views FROM \"SongsListened\" sl1, LATERAL (SELECT COUNT(*) FROM \"SongsListened\" s WHERE sl1.\"songId\" = s.\"songId\" AND s.\"listenerId\" = '{listenerId}' AND s.\"listenedAt\" >= (NOW()::TIMESTAMP - interval '1' day * '7')) sl2 WHERE sl2.count != 0 GROUP BY views, \"songId\" ORDER BY views DESC LIMIT '30'";

            List<ItemFromEnumWithViews> songs = npgsqlDbConnection.Query<ItemFromEnumWithViews>(sql).ToList();

            return songs;
        }

        public static List<ItemFromEnumWithViews> GetListenerForgottenPopularSongs(int listenerId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT \"songId\" as \"itemId\", sl2.count AS views FROM \"SongsListened\" sl1, LATERAL (SELECT COUNT(*), MAX(s.\"listenedAt\") AS \"maxListenedAt\" FROM \"SongsListened\" s WHERE sl1.\"songId\" = s.\"songId\" AND s.\"listenerId\" = '{listenerId}') sl2 WHERE sl2.count != 0 AND sl2.\"maxListenedAt\" <= (NOW()::TIMESTAMP - interval '1' day * '7') GROUP BY views, \"songId\" ORDER BY views DESC LIMIT '30'";

            List<ItemFromEnumWithViews> songs = npgsqlDbConnection.Query<ItemFromEnumWithViews>(sql).ToList();

            return songs;
        }

        public static List<ItemFromEnum> GetListenerHistorySongs(int listenerId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT sl.\"songId\" AS \"itemId\" FROM \"SongsListened\" sl WHERE sl.\"listenerId\" = '{listenerId}' ORDER BY sl.\"listenedAt\" DESC LIMIT '30'";

            List<ItemFromEnum> songs = npgsqlDbConnection.Query<ItemFromEnum>(sql).ToList();

            return songs;
        }

        public static List<ItemFromEnumWithViews> GetArtistMostPopularSongs(int artistId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT s2.id AS \"itemId\", SUM(sl2.count) AS views FROM  \"Songs\" s2 INNER JOIN \"Discography\" d ON d.id = s2.\"discId\", LATERAL (SELECT COUNT(*) FROM \"SongsListened\" s WHERE s.\"songId\" = s2.id AND d.\"artistId\" = '{artistId}') sl2 WHERE d.\"artistId\" = '{artistId}' GROUP BY s2.id ORDER BY views DESC LIMIT '10'";

            List<ItemFromEnumWithViews> songs = npgsqlDbConnection.Query<ItemFromEnumWithViews>(sql).ToList();

            return songs;
        }

        public static List<ItemFromEnum> GetAllArtistSingles(int artistId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT d.id AS \"itemId\" FROM \"Discography\" d WHERE d.\"artistId\" = '{artistId}' AND (SELECT COUNT(*) FROM \"Songs\" s WHERE s.\"discId\" = d.id) = '1'";

            List<ItemFromEnum> singles = npgsqlDbConnection.Query<ItemFromEnum>(sql).ToList();

            return singles;
        }

        public static List<ItemFromEnum> GetAllArtistAlbums(int artistId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT d.id AS \"itemId\" FROM \"Discography\" d WHERE d.\"artistId\" = '{artistId}' AND (SELECT COUNT(*) FROM \"Songs\" s WHERE s.\"discId\" = d.id) > '1'";

            List<ItemFromEnum> albums = npgsqlDbConnection.Query<ItemFromEnum>(sql).ToList();

            return albums;
        }

        public static List<ItemFromEnum> GetArtistTheMostPopularDiscography(int artistId, NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT d.id AS \"itemId\", SUM(sl2.count) AS views FROM  \"Songs\" s2 INNER JOIN \"Discography\" d ON d.id = s2.\"discId\", LATERAL (SELECT COUNT(*) FROM \"SongsListened\" s WHERE s.\"songId\" = s2.id AND d.\"artistId\" = '{artistId}') sl2 WHERE d.\"artistId\" = '{artistId}' GROUP BY d.id ORDER BY views DESC";

            List<ItemFromEnum> albums = npgsqlDbConnection.Query<ItemFromEnum>(sql).ToList();

            return albums;
        }

        public static Song GetOneRandomSong(NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"SELECT * FROM \"Songs\" LIMIT 1";

            var song = npgsqlDbConnection.QuerySingle<Song>(sql);
            return song;
        }

        public static List<ItemFromEnum> GetRandomSongs(NpgsqlConnection npgsqlDbConnection)
        {
            var sql = $"WITH params AS (SELECT 1 AS min_id, (SELECT MAX(s2.id) FROM \"Songs\" s2).max AS id_span) SELECT r.id AS \"itemId\" FROM  (SELECT p.min_id + trunc(random() * p.id_span)::integer AS id FROM params p, generate_series(1, p.id_span) g GROUP BY 1) r JOIN \"Songs\" USING (id) LIMIT  10";

            List<ItemFromEnum> songs = npgsqlDbConnection.Query<ItemFromEnum>(sql).ToList();

            if (songs.Count == 0 && GetOneRandomSong(npgsqlDbConnection) != null)
            {
                return GetRandomSongs(npgsqlDbConnection);
            }

            return songs;
        }

        private static List<ItemFromEnum> Search(string searchStr, string tableName, NpgsqlConnection npgsqlConnection)
        {
            string regexPattern = "[^\\w\\s'`\"]+";

            string[] searchStringSimplified = Regex.Split(searchStr, regexPattern);

            string whereStr = "";
            string orderByStr = "";

            for (int i = 0; i < searchStringSimplified.Length; i++)
            {
                string wordInLower = searchStringSimplified[i].ToLower();

                if (i == 0)
                {
                    whereStr += $"WHERE lname LIKE '%{wordInLower}%'";
                    orderByStr += $"ORDER BY CASE WHEN lname LIKE '%{wordInLower}%' THEN 1 ELSE 2 END";
                } else
                {
                    whereStr += $" OR lname LIKE '%{wordInLower}%'";
                    orderByStr += $" CASE WHEN lname LIKE '%{wordInLower}%' THEN 1 ELSE 2 END";
                }
            }

            var sql = $"SELECT id as \"itemId\" FROM \"{tableName}\", LOWER(name) AS lname {whereStr} {orderByStr} LIMIT '30'";

            List<ItemFromEnum> items = npgsqlConnection.Query<ItemFromEnum>(sql).ToList();

            return items;
        }

        private static List<ItemFromEnum> SearchInLibrary(int userId, string searchStr, string tableName, string followersTableName, NpgsqlConnection npgsqlConnection)
        {
            string regexPattern = "[^\\w\\s'`\"]+";

            string[] searchStringSimplified = Regex.Split(searchStr, regexPattern);

            string whereStr = "";
            string orderByStr = "";

            for (int i = 0; i < searchStringSimplified.Length; i++)
            {
                string wordInLower = searchStringSimplified[i].ToLower();

                if (i == 0)
                {
                    whereStr += $"WHERE ef.\"followerId\" = {userId} AND lname LIKE '%{wordInLower}%'";
                    orderByStr += $"ORDER BY CASE WHEN lname LIKE '%{wordInLower}%' THEN 1 ELSE 2 END";
                }
                else
                {
                    whereStr += $" OR lname LIKE '%{wordInLower}%'";
                    orderByStr += $" CASE WHEN lname LIKE '%{wordInLower}%' THEN 1 ELSE 2 END";
                }
            }

            var sql = $"SELECT e.id as \"itemId\" FROM \"{tableName}\" e INNER JOIN \"{followersTableName}\" ef ON ef.\"elementId\" = e.id, LOWER(name) AS lname {whereStr} {orderByStr} LIMIT '30'";

            List<ItemFromEnum> items = npgsqlConnection.Query<ItemFromEnum>(sql).ToList();

            return items;
        }

        public static List<ItemFromEnum> SearchForSongs(string searchStr, NpgsqlConnection npgsqlConnection)
        {
            return Search(searchStr, "Songs", npgsqlConnection);
        }

        public static List<ItemFromEnum> SearchForDiscs(string searchStr, NpgsqlConnection npgsqlConnection)
        {
            return Search(searchStr, "Discography", npgsqlConnection);
        }

        public static List<ItemFromEnum> SearchForArtists(string searchStr, NpgsqlConnection npgsqlConnection)
        {
            return Search(searchStr, "Users", npgsqlConnection);
        }

        public static List<ItemFromEnum> SearchForSongsInLibrary(int userId, string searchStr, NpgsqlConnection npgsqlConnection)
        {
            return SearchInLibrary(userId, searchStr, "Songs", "SongsFollowers", npgsqlConnection);
        }

        public static List<ItemFromEnum> SearchForDiscsInLibrary(int userId, string searchStr, NpgsqlConnection npgsqlConnection)
        {
            return SearchInLibrary(userId, searchStr, "Discography", "DiscographyFollowers", npgsqlConnection);
        }

        public static List<ItemFromEnum> SearchForArtistsInLibrary(int userId, string searchStr, NpgsqlConnection npgsqlConnection)
        {
            return SearchInLibrary(userId, searchStr, "Users", "ArtistsFollowers", npgsqlConnection);
        }

        public static long GetArtistMouthlyListeners(int artistId, NpgsqlConnection npgsqlConnection)
        {
            var sql = $"SELECT COUNT(*) AS \"Count\" FROM (SELECT sl.\"listenerId\" FROM \"SongsListened\" sl INNER JOIN \"Songs\" s ON s.id = sl.\"songId\" INNER JOIN \"Discography\" d ON d.id = s.\"discId\" INNER JOIN \"Users\" a ON a.id = d.\"artistId\" WHERE a.id = '{artistId}' AND sl.\"listenedAt\" >= (NOW()::TIMESTAMP - INTERVAL '1' MONTH) GROUP BY sl.\"listenerId\") sl1";

            var artistListenersCount = npgsqlConnection.QuerySingle(sql);

            return artistListenersCount.Count;
        }
    }
}

