/****** Object:  StoredProcedure [dbo].[drupal_imis_isgweb_sync_cleanup]    Script Date: 10/07/2011 10:55:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[drupal_imis_isgweb_sync_cleanup]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[drupal_imis_isgweb_sync_cleanup]
GO
/****** Object:  View [dbo].[drupal_imis_isgweb_users]    Script Date: 10/07/2011 10:55:15 ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[drupal_imis_isgweb_users]'))
DROP VIEW [dbo].[drupal_imis_isgweb_users]
GO
/****** Object:  StoredProcedure [dbo].[drupal_imis_isgweb_sync]    Script Date: 10/07/2011 10:55:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[drupal_imis_isgweb_sync]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[drupal_imis_isgweb_sync]
GO
/****** Object:  StoredProcedure [dbo].[drupal_imis_isgweb_sync]    Script Date: 10/07/2011 10:55:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[drupal_imis_isgweb_sync]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'
CREATE PROC [dbo].[drupal_imis_isgweb_sync] 
(
	@ID AS VARCHAR(10) = '''',
	@LAST_UPDATED AS VARCHAR(10) = ''''
)
AS
BEGIN
	
	SET @ID = LTRIM(RTRIM(@ID))
	SET @LAST_UPDATED = LTRIM(RTRIM(@LAST_UPDATED))
	
	DECLARE @sql AS NVARCHAR(4000)
	
	SET @sql = ''
		CREATE TABLE #GROUPER (id_null int)
		
		SELECT	id_null,
				ID,
				LAST_UPDATED,
				LOGIN_DISABLED,
				WEB_LOGIN,
				EMAIL,
				FIRST_NAME,
				LAST_NAME
		FROM	#GROUPER AS [Users]
		RIGHT OUTER JOIN (
				SELECT	[User].ID,
						[User].LAST_UPDATED,
						[User].STATUS,
						[User].LOGIN_DISABLED,
						[User].WEB_LOGIN,
						[User].EMAIL,
						[User].FIRST_NAME,
						[User].LAST_NAME
				FROM	drupal_imis_isgweb_users AS [User]
				WHERE	1 = 1
				AND		[User].LAST_NAME <> ''''''''
				AND		[User].FIRST_NAME <> ''''''''
		''
		
	IF @LAST_UPDATED <> '''' SET @sql = @sql + ''
				AND		[User].LAST_UPDATED >= ''''''+@LAST_UPDATED+'''''' ''
		
	IF @ID <> '''' SET @sql = @sql + ''
				AND		[User].ID = ''''''+@ID+'''''' ''
		
	SET @sql = @sql + ''
				) AS [User] ON [Users].id_null = [User].ID
		FOR XML AUTO, ELEMENTS
		
		DROP TABLE #GROUPER
		''
		
	EXEC sp_executesql @sql
	-- PRINT @sql
	
END

' 
END
GO
/****** Object:  View [dbo].[drupal_imis_isgweb_users]    Script Date: 10/07/2011 10:55:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[drupal_imis_isgweb_users]'))
EXEC dbo.sp_executesql @statement = N'
CREATE VIEW [dbo].[drupal_imis_isgweb_users]
AS
	SELECT	Name.ID,
			Name.LAST_UPDATED,
			Name.STATUS,
			Name_Security.LOGIN_DISABLED,
			Name_Security.WEB_LOGIN,
			Name.EMAIL,
			Name.FIRST_NAME,
			Name.LAST_NAME
	FROM	Name
	JOIN	Name_Security ON Name_Security.ID = Name.ID
	WHERE	Name_Security.ID = Name.ID 
	AND		Name_Security.WEB_LOGIN <> ''''
	
'
GO
/****** Object:  StoredProcedure [dbo].[drupal_imis_isgweb_sync_cleanup]    Script Date: 10/07/2011 10:55:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[drupal_imis_isgweb_sync_cleanup]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROC [dbo].[drupal_imis_isgweb_sync_cleanup]
AS
BEGIN
	CREATE TABLE #GROUPER (id_null int)
	
	SELECT	[Users].id_null,
			[User].ID
	FROM	#GROUPER AS [Users]
	RIGHT OUTER JOIN Name AS [User] ON [User].ID = Users.id_null
	WHERE	[User].ID NOT IN (SELECT ID FROM drupal_imis_isgweb_users)
	FOR XML AUTO, ELEMENTS
	
	DROP TABLE #GROUPER
	
END' 
END
GO
