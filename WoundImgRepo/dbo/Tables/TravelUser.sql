CREATE TABLE [dbo].[TravelUser] (
    [UserId]    VARCHAR (10)   NOT NULL,
    [UserPw]    VARBINARY (50) NOT NULL,
    [FullName]  VARCHAR (50)   NOT NULL,
    [Email]     VARCHAR (50)   NOT NULL,
    [Dob]       DATE           NOT NULL,
    [UserRole]  VARCHAR (20)   NOT NULL,
    [LastLogin] DATETIME       NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC)
);

