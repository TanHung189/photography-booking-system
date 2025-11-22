using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PhotoBooking.Models;

public partial class PhotoBookingContext : DbContext
{
    public PhotoBookingContext()
    {
    }

    public PhotoBookingContext(DbContextOptions<PhotoBookingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AlbumAnh> AlbumAnhs { get; set; }

    public virtual DbSet<AnhChiTiet> AnhChiTiets { get; set; }

    public virtual DbSet<DanhGium> DanhGia { get; set; }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<DiaDiem> DiaDiems { get; set; }

    public virtual DbSet<DonDatLich> DonDatLiches { get; set; }

    public virtual DbSet<GoiDichVu> GoiDichVus { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=WINDOWS-10\\SQLEXPRESS;Database=PhotoBookingTH;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlbumAnh>(entity =>
        {
            entity.HasKey(e => e.MaAlbum).HasName("PK__AlbumAnh__0AE6D6CC2814B20C");

            entity.ToTable("AlbumAnh");

            entity.Property(e => e.TieuDe).HasMaxLength(200);

            entity.HasOne(d => d.MaNhiepAnhGiaNavigation).WithMany(p => p.AlbumAnhs)
                .HasForeignKey(d => d.MaNhiepAnhGia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AlbumAnh__MaNhie__534D60F1");
        });

        modelBuilder.Entity<AnhChiTiet>(entity =>
        {
            entity.HasKey(e => e.MaAnh).HasName("PK__AnhChiTi__356240DFF7787CAE");

            entity.ToTable("AnhChiTiet");

            entity.HasOne(d => d.MaAlbumNavigation).WithMany(p => p.AnhChiTiets)
                .HasForeignKey(d => d.MaAlbum)
                .HasConstraintName("FK__AnhChiTie__MaAlb__5629CD9C");
        });

        modelBuilder.Entity<DanhGium>(entity =>
        {
            entity.HasKey(e => e.MaDanhGia).HasName("PK__DanhGia__AA9515BF9557D6FA");

            entity.HasIndex(e => e.MaDon, "UQ__DanhGia__3D89F569CA2EC6A4").IsUnique();

            entity.Property(e => e.BinhLuan).HasMaxLength(1000);
            entity.Property(e => e.NgayDanhGia).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PhanHoi).HasMaxLength(1000);

            entity.HasOne(d => d.MaDonNavigation).WithOne(p => p.DanhGium)
                .HasForeignKey<DanhGium>(d => d.MaDon)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DanhGia__MaDon__5070F446");
        });

        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.MaDanhMuc).HasName("PK__DanhMuc__B37508876B3B4697");

            entity.ToTable("DanhMuc");

            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.TenDanhMuc).HasMaxLength(100);
        });

        modelBuilder.Entity<DiaDiem>(entity =>
        {
            entity.HasKey(e => e.MaDiaDiem).HasName("PK__DiaDiem__F015962A9E2B7F13");

            entity.ToTable("DiaDiem");

            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.Property(e => e.TenThanhPho).HasMaxLength(100);
        });

        modelBuilder.Entity<DonDatLich>(entity =>
        {
            entity.HasKey(e => e.MaDon).HasName("PK__DonDatLi__3D89F56822DBCE65");

            entity.ToTable("DonDatLich");

            entity.Property(e => e.DiaChiChup).HasMaxLength(200);
            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TienDaCoc).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.MaGoiNavigation).WithMany(p => p.DonDatLiches)
                .HasForeignKey(d => d.MaGoi)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DonDatLic__MaGoi__49C3F6B7");

            entity.HasOne(d => d.MaKhachHangNavigation).WithMany(p => p.DonDatLiches)
                .HasForeignKey(d => d.MaKhachHang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DonDatLic__MaKha__4AB81AF0");
        });

        modelBuilder.Entity<GoiDichVu>(entity =>
        {
            entity.HasKey(e => e.MaGoi).HasName("PK__GoiDichV__3CD30F6974A08C92");

            entity.ToTable("GoiDichVu");

            entity.Property(e => e.GiaCoc)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiaTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SoNguoiToiDa).HasDefaultValue(1);
            entity.Property(e => e.TenGoi).HasMaxLength(200);

            entity.HasOne(d => d.MaDanhMucNavigation).WithMany(p => p.GoiDichVus)
                .HasForeignKey(d => d.MaDanhMuc)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GoiDichVu__MaDan__4316F928");

            entity.HasOne(d => d.MaNhiepAnhGiaNavigation).WithMany(p => p.GoiDichVus)
                .HasForeignKey(d => d.MaNhiepAnhGia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GoiDichVu__MaNhi__440B1D61");
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__NguoiDun__C539D762ED5A0852");

            entity.ToTable("NguoiDung");

            entity.HasIndex(e => e.TenDangNhap, "UQ__NguoiDun__55F68FC089DFF6C5").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__NguoiDun__A9D105347269FA07").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HoVaTen).HasMaxLength(100);
            entity.Property(e => e.MatKhau).HasMaxLength(100);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);
            entity.Property(e => e.TenDangNhap).HasMaxLength(50);
            entity.Property(e => e.VaiTro).HasMaxLength(20);

            entity.HasOne(d => d.MaDiaDiemNavigation).WithMany(p => p.NguoiDungs)
                .HasForeignKey(d => d.MaDiaDiem)
                .HasConstraintName("FK__NguoiDung__MaDia__3C69FB99");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
