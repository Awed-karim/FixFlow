import { useState } from 'react'
import { useDropzone } from 'react-dropzone'

export default function PhotoDropzone({
  label,
  onFiles,
  disabled,
}: {
  label: string
  onFiles: (files: File[]) => void
  disabled?: boolean
}) {
  const [error, setError] = useState('')

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    accept: {
      'image/jpeg': ['.jpg', '.jpeg'],
      'image/png': ['.png'],
      'image/webp': ['.webp'],
    },
    maxSize: 5 * 1024 * 1024,
    disabled,
    onDropAccepted: (files) => {
      setError('')
      onFiles(files)
    },
    onDropRejected: () => setError('الملف لازم يكون صورة (JPG / PNG / WEBP) وأقل من 5 ميجا'),
  })

  return (
    <div>
      <div
        {...getRootProps()}
        className={`cursor-pointer rounded-xl border-2 border-dashed p-6 text-center text-sm transition ${
          disabled
            ? 'cursor-not-allowed border-slate-200 bg-slate-50 text-slate-400'
            : isDragActive
              ? 'border-blue-500 bg-blue-50 text-blue-700'
              : 'border-slate-300 bg-white text-slate-600 hover:border-blue-400'
        }`}
      >
        <input {...getInputProps()} />
        <p className="font-semibold">{label}</p>
        <p className="mt-1 text-xs">
          {isDragActive ? 'سيب الصور هنا…' : 'اسحب الصور هنا أو اضغط للاختيار'}
        </p>
      </div>
      {error && <p className="mt-2 text-xs text-red-600">{error}</p>}
    </div>
  )
}