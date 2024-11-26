# from pptx import Presentation, parts
# from pptx.enum.shapes import MSO_SHAPE_TYPE

# pptx_file = "C:/Users/daidu/Projects/slide-generation/backend/template/test.pptx"  # File PowerPoint gốc
# new_image_path = "C:/Users/daidu/Projects/slide-generation/backend/images/data_images/0.webp"  # Ảnh thay thế
# output_file = "C:/Users/daidu/Projects/slide-generation/backend/images/data_images/new1.pptx"  # File PowerPoint kết quả

# prs = Presentation(pptx_file)
# count = 1
# slide = prs.slides[0]
# print(slide.shapes[0])
# for shape in slide.shapes:
#     if shape.shape_type == MSO_SHAPE_TYPE.PICTURE:
#         im = parts.image.Image.from_file(new_image_path)
#         slide_part, rId = shape.part, shape._element.blip_rId
#         image_part = slide_part.related_part(rId)
#         image_part.blob = im._blob
#         print (">>> im", im)
#         count += 1

# prs.save(output_file)



from pptx_replace import Presentation

# Đường dẫn file PPTX nguồn và file đầu ra
pptx_file = "C:/Users/daidu/Projects/slide-generation/backend/template/test.pptx"  # File PowerPoint gốc
new_image_path = "C:/Users/daidu/Projects/slide-generation/backend/images/data_images/0.webp"  # Ảnh thay thế
output_file = "C:/Users/daidu/Projects/slide-generation/backend/images/data_images/new1.pptx"  # File PowerPoint kết quả

# Tạo một đối tượng Presentation
prs = Presentation(pptx_file)

# Dữ liệu thay thế
replacements = {
    "{{IMAGE_PLACEHOLDER}}": {
        "type": "image",
        "path": new_image_path,  # Đường dẫn ảnh thay thế
    }
}

# Thực hiện thay thế
prs.replace(replacements)

# Lưu file PowerPoint đã thay đổi
prs.save(output_file)

print(f"Đã thay thế ảnh và lưu vào {output_file}")
