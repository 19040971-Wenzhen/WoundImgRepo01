CREATE TABLE [dbo].[TravelHighlight] (
    [Id]       INT            IDENTITY (1, 1) NOT NULL,
    [Title]    VARCHAR (100)  NOT NULL,
    [City]     VARCHAR (70)   NOT NULL,
    [TripDate] DATE           NOT NULL,
    [Duration] INT            NOT NULL,
    [Spending] FLOAT (53)     NOT NULL,
    [Story]    VARCHAR (2000) NOT NULL,
    [Picture]  VARCHAR (70)   NOT NULL,
    [UserId]   VARCHAR (10)   NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [p09fk] FOREIGN KEY ([UserId]) REFERENCES [dbo].[TravelUser] ([UserId])
);

