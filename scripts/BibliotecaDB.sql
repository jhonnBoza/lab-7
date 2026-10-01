/*
    BibliotecaDB
    Semana 07 - Arquitectura de capas
    Ejecutar en (localdb)\MSSQLLocalDB
*/
USE master;
GO

IF DB_ID(N'BibliotecaDB') IS NOT NULL
BEGIN
    ALTER DATABASE BibliotecaDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE BibliotecaDB;
END
GO

CREATE DATABASE BibliotecaDB;
GO

USE BibliotecaDB;
GO

CREATE TABLE Autores
(
    AutorId       INT IDENTITY(1, 1) NOT NULL,
    Nombre        NVARCHAR(120)      NOT NULL,
    Nacionalidad  NVARCHAR(80)       NOT NULL,
    Activo        BIT                NOT NULL CONSTRAINT DF_Autores_Activo DEFAULT (1),
    CONSTRAINT PK_Autores PRIMARY KEY (AutorId)
);
GO

CREATE TABLE Libros
(
    LibroId     INT IDENTITY(1, 1) NOT NULL,
    Titulo      NVARCHAR(200)      NOT NULL,
    ISBN        NVARCHAR(20)       NOT NULL,
    AutorId     INT                NOT NULL,
    Ejemplares  INT                NOT NULL,
    Activo      BIT                NOT NULL CONSTRAINT DF_Libros_Activo DEFAULT (1),
    CONSTRAINT PK_Libros PRIMARY KEY (LibroId),
    CONSTRAINT UQ_Libros_ISBN UNIQUE (ISBN),
    CONSTRAINT FK_Libros_Autores FOREIGN KEY (AutorId) REFERENCES Autores (AutorId),
    CONSTRAINT CK_Libros_Ejemplares CHECK (Ejemplares >= 0)
);
GO

CREATE TABLE Socios
(
    SocioId  INT IDENTITY(1, 1) NOT NULL,
    DNI      NVARCHAR(15)       NOT NULL,
    Nombre   NVARCHAR(120)      NOT NULL,
    Email    NVARCHAR(120)      NOT NULL,
    Activo   BIT                NOT NULL CONSTRAINT DF_Socios_Activo DEFAULT (1),
    CONSTRAINT PK_Socios PRIMARY KEY (SocioId),
    CONSTRAINT UQ_Socios_DNI UNIQUE (DNI)
);
GO

CREATE TABLE Prestamos
(
    PrestamoId     INT IDENTITY(1, 1) NOT NULL,
    SocioId        INT                NOT NULL,
    FechaPrestamo  DATETIME           NOT NULL,
    FechaLimite    DATETIME           NOT NULL,
    Estado         NVARCHAR(20)       NOT NULL,
    CONSTRAINT PK_Prestamos PRIMARY KEY (PrestamoId),
    CONSTRAINT FK_Prestamos_Socios FOREIGN KEY (SocioId) REFERENCES Socios (SocioId),
    CONSTRAINT CK_Prestamos_Estado CHECK (Estado IN (N'Pendiente', N'Devuelto'))
);
GO

CREATE TABLE DetallePrestamo
(
    PrestamoId       INT      NOT NULL,
    LibroId          INT      NOT NULL,
    FechaDevolucion  DATETIME NULL,
    CONSTRAINT PK_DetallePrestamo PRIMARY KEY (PrestamoId, LibroId),
    CONSTRAINT FK_DetallePrestamo_Prestamos FOREIGN KEY (PrestamoId) REFERENCES Prestamos (PrestamoId),
    CONSTRAINT FK_DetallePrestamo_Libros FOREIGN KEY (LibroId) REFERENCES Libros (LibroId)
);
GO

INSERT INTO Autores (Nombre, Nacionalidad) VALUES
(N'Gabriel García Márquez', N'Colombia'),
(N'Mario Vargas Llosa', N'Perú'),
(N'Julio Cortázar', N'Argentina'),
(N'Isabel Allende', N'Chile'),
(N'Jorge Luis Borges', N'Argentina'),
(N'Laura Esquivel', N'México'),
(N'Pablo Neruda', N'Chile'),
(N'Miguel de Cervantes', N'España');
GO

INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares) VALUES
(N'Cien años de soledad', N'9780307474728', 1, 5),
(N'El amor en los tiempos del cólera', N'9780307389732', 1, 3),
(N'Crónica de una muerte anunciada', N'9781400034710', 1, 2),
(N'El coronel no tiene quien le escriba', N'9780307474582', 1, 4),
(N'La ciudad y los perros', N'9788420412141', 2, 4),
(N'Conversación en La Catedral', N'9788420437488', 2, 3),
(N'La fiesta del chivo', N'9788420471833', 2, 2),
(N'Los cachorros', N'9788420412151', 2, 0),
(N'Rayuela', N'9788420437481', 3, 3),
(N'Bestiario', N'9788466338145', 3, 2),
(N'La casa de los espíritus', N'9780525433477', 4, 4),
(N'De amor y de sombra', N'9780553383805', 4, 2),
(N'Ficciones', N'9780802130303', 5, 5),
(N'El Aleph', N'9788426406486', 5, 3),
(N'Como agua para chocolate', N'9780385420174', 6, 4),
(N'La ley del amor', N'9780609801279', 6, 2),
(N'Veinte poemas de amor', N'9788498959716', 7, 6),
(N'Canto general', N'9788437604945', 7, 2),
(N'Don Quijote de la Mancha', N'9788420412144', 8, 5),
(N'Novelas ejemplares', N'9788437601906', 8, 2);
GO

INSERT INTO Socios (DNI, Nombre, Email) VALUES
(N'45678901', N'Ana Torres', N'ana.torres@correo.com'),
(N'12345678', N'Luis Pérez', N'luis.perez@correo.com'),
(N'87654321', N'María Quispe', N'maria.quispe@correo.com'),
(N'11223344', N'José Ramírez', N'jose.ramirez@correo.com'),
(N'99887766', N'Carmen Díaz', N'carmen.diaz@correo.com'),
(N'44556677', N'Pedro Rojas', N'pedro.rojas@correo.com'),
(N'33445566', N'Lucía Vargas', N'lucia.vargas@correo.com'),
(N'22334455', N'Diego Salazar', N'diego.salazar@correo.com'),
(N'66778899', N'Rosa Mendoza', N'rosa.mendoza@correo.com'),
(N'10293847', N'Hugo Castillo', N'hugo.castillo@correo.com');
GO

/* Ana Torres: 3 libros pendientes, para probar el tope de 3. */
INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado)
VALUES (1, DATEADD(DAY, -2, CAST(GETDATE() AS DATE)), DATEADD(DAY, 5, CAST(GETDATE() AS DATE)), N'Pendiente');

/* Luis Pérez: 1 libro pendiente y vencido, para probar la multa. */
INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado)
VALUES (2, DATEADD(DAY, -10, CAST(GETDATE() AS DATE)), DATEADD(DAY, -3, CAST(GETDATE() AS DATE)), N'Pendiente');

/* María Quispe: un libro ya devuelto y otro todavía pendiente. */
INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado)
VALUES (3, DATEADD(DAY, -8, CAST(GETDATE() AS DATE)), DATEADD(DAY, -1, CAST(GETDATE() AS DATE)), N'Pendiente');

/* José Ramírez: préstamo completamente devuelto. */
INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado)
VALUES (4, DATEADD(DAY, -20, CAST(GETDATE() AS DATE)), DATEADD(DAY, -13, CAST(GETDATE() AS DATE)), N'Devuelto');

/* Carmen Díaz: 1 libro pendiente dentro del plazo. */
INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado)
VALUES (5, DATEADD(DAY, -1, CAST(GETDATE() AS DATE)), DATEADD(DAY, 6, CAST(GETDATE() AS DATE)), N'Pendiente');
GO

INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
(1, 1, NULL),
(1, 5, NULL),
(1, 9, NULL),
(2, 13, NULL),
(3, 11, DATEADD(DAY, -1, CAST(GETDATE() AS DATE))),
(3, 17, NULL),
(4, 14, DATEADD(DAY, -10, CAST(GETDATE() AS DATE))),
(5, 18, NULL);
GO

SELECT N'Autores' AS Tabla, COUNT(*) AS Total FROM Autores
UNION ALL SELECT N'Libros', COUNT(*) FROM Libros
UNION ALL SELECT N'Socios', COUNT(*) FROM Socios
UNION ALL SELECT N'Prestamos', COUNT(*) FROM Prestamos
UNION ALL SELECT N'DetallePrestamo', COUNT(*) FROM DetallePrestamo;
GO

SELECT s.Nombre, COUNT(*) AS LibrosPendientes
FROM Socios s
INNER JOIN Prestamos p ON p.SocioId = s.SocioId
INNER JOIN DetallePrestamo d ON d.PrestamoId = p.PrestamoId
WHERE d.FechaDevolucion IS NULL
GROUP BY s.Nombre;
GO
