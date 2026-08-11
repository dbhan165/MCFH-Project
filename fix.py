import codecs
content = codecs.open('d:/MCFH-Project/MCFH/Controllers/Projects/ProjectDataSourceController.cs', 'r', 'utf-8').read()
content = content.replace('    [HttpDelete(imports/{fileId})]', '    [HttpDelete("imports/{fileId}")]')
content = content.replace('Không tìm th?y file import ho?c không có quy?n.', '"Không tìm thấy file import hoặc không có quyền."')
content = content.replace('Xóa file import thành công.', '"Xóa file import thành công."')
content = content.replace('_dataSourceService.DeleteImportFileAsync', '_service.DeleteImportFileAsync')
codecs.open('d:/MCFH-Project/MCFH/Controllers/Projects/ProjectDataSourceController.cs', 'w', 'utf-8').write(content)
